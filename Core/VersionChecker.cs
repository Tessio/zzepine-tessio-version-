using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace GTAVInjector.Core
{
    public static class VersionChecker
    {
        // URL del repositorio de GitHub para verificación de versiones
        private const string VERSION_JSON_URL = "https://raw.githubusercontent.com/Tessio/Translations/refs/heads/master/version_l.txt";
        private const string TESSIO_DISCORD_URL = "https://gtaggs.wirdland.xyz/discord";
        
        // NOTA: La versión actual ahora se obtiene directamente del Assembly (definida en el .csproj)
        // Ya no dependemos del archivo version.txt - La versión se define en una sola ubicación
        
        private static string? _latestVersion;
        private static bool _isOutdated = false;
        private static readonly HttpClient _httpClient = new();
        private static System.Threading.Timer? _versionTimer;

        // VERSIÓN FIJA DESDE CSPROJ - NO USAR ASSEMBLY
        private static string GetCurrentVersionFromProject()
        {
            // Versión exacta del .csproj (debe actualizarse manualmente aquí)
            return "1.0.7"; // ⚠️ ACTUALIZAR ESTO CUANDO CAMBIES LA VERSIÓN DEL PROYECTO
        }
        public static async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                // OBTENER CONTENIDO DIRECTO DEL ENLACE GITHUB (SIN CACHE NI HEADERS)
                var githubVersion = await _httpClient.GetStringAsync(VERSION_JSON_URL);
                _latestVersion = githubVersion.Trim();

                // OBTENER VERSIÓN DEL PROYECTO
                var currentVersion = GetCurrentVersionFromProject();

                System.Diagnostics.Debug.WriteLine($"📱 VERSIÓN DEL PROYECTO: '{currentVersion}'");
                System.Diagnostics.Debug.WriteLine($"🌐 VERSIÓN DE GITHUB: '{_latestVersion}'");

                if (!string.IsNullOrEmpty(_latestVersion))
                {
                    // COMPARACIÓN SIMPLE DE VERSIONES
                    var current = new Version(currentVersion);
                    var latest = new Version(_latestVersion);

                    _isOutdated = current < latest;
                    
                    System.Diagnostics.Debug.WriteLine($"🔍 COMPARACIÓN: {currentVersion} < {_latestVersion} = {_isOutdated}");
                    
                    return _isOutdated;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex.Message}");
                return false;
            }
        }

        public static void OpenDiscordUpdate()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TESSIO_DISCORD_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open Discord: {ex.Message}");
            }
        }

        public static string GetCurrentVersion()
        {
            return GetCurrentVersionFromProject();
        }

        public static string? GetLatestVersion()
        {
            return _latestVersion;
        }

        public static bool IsOutdated()
        {
            return _isOutdated;
        }

        // Timer para verificar constantemente las actualizaciones
        public static void StartVersionMonitoring(Action<bool> onVersionChanged)
        {
            // Detener timer anterior si existe
            _versionTimer?.Dispose();
            
            _versionTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    bool wasOutdated = _isOutdated;
                    await CheckForUpdatesAsync();
                    
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    System.Diagnostics.Debug.WriteLine($"⏱️ [{timestamp}] Timer ejecutado - Estado anterior: {wasOutdated}, Estado actual: {_isOutdated}");
                    
                    // También escribir a un archivo log temporal para verificación
                    try 
                    {
                        System.IO.File.AppendAllText("tmp_rovodev_version_log.txt", 
                            $"[{timestamp}] VersionChecker ejecutado - Versión actual: {GetCurrentVersionFromProject()}, Versión remota: {_latestVersion}, Desactualizada: {_isOutdated}\n");
                    }
                    catch { /* Ignorar errores de escritura */ }
                    
                    if (wasOutdated != _isOutdated)
                    {
                        onVersionChanged?.Invoke(_isOutdated);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en timer: {ex.Message}");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10)); // Verificar cada 10 segundos
        }
        
        // Método para detener el monitoreo
        public static void StopVersionMonitoring()
        {
            _versionTimer?.Dispose();
            _versionTimer = null;
        }
    }
}
