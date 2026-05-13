using Plugin.Maui.Audio;
using System.Diagnostics;

namespace ProjektMooPing.Services
{
    public static class SoundService
    {
        private static IAudioManager _audioManager = AudioManager.Current;
        private static IAudioPlayer _singleSfxPlayer;
        private static IAudioPlayer _bgmPlayer;

        public static async Task PlaySfx(string fileName)
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                var player = _audioManager.CreatePlayer(stream);
                player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing SFX '{fileName}': {ex.Message}");
            }
        }

        // --- 2. แบบห้ามซ้อน (ถ้ากดใหม่ ของเก่าต้องหยุดทันที) ---
        public static async Task PlaySfxSingle(string fileName)
        {
            try
            {
                if (_singleSfxPlayer != null)
                {
                    _singleSfxPlayer.Stop();
                    _singleSfxPlayer.Dispose();
                    _singleSfxPlayer = null;
                }

                var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                _singleSfxPlayer = _audioManager.CreatePlayer(stream);
                _singleSfxPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing single SFX '{fileName}': {ex.Message}");
            }
        }

        public static async Task PlayBgm(string fileName, double volume = 0.3)
        {
            try
            {
                if (_bgmPlayer != null && _bgmPlayer.IsPlaying) return;

                _bgmPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(fileName));
                _bgmPlayer.Loop = true;
                _bgmPlayer.Volume = volume;
                _bgmPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing BGM '{fileName}': {ex.Message}");
            }
        }

        public static async void StopBgm(int durationMilliseconds = 1500)
        {
            try
            {
                if (_bgmPlayer == null || !_bgmPlayer.IsPlaying) return;

                int steps = 20;
                int delayBetweenSteps = durationMilliseconds / steps;
                double volumeStep = _bgmPlayer.Volume / steps;

                for (int i = 0; i < steps; i++)
                {
                    double newVolume = _bgmPlayer.Volume - volumeStep;
                    if (newVolume < 0) newVolume = 0;

                    _bgmPlayer.Volume = newVolume;
                    await Task.Delay(delayBetweenSteps);
                }

                _bgmPlayer.Stop();
                _bgmPlayer.Dispose();
                _bgmPlayer = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping BGM: {ex.Message}");
            }
        }

        public static void PlayBGM() => _ = PlayBgm("bgm.mp3");
        public static void PlayCashRegister() => _ = PlaySfx("chaching.mp3");
        public static void PlayClick1() => _ = PlaySfx("click1.mp3");
        public static void PlayClick2() => _ = PlaySfx("click2.mp3");
        public static void PlayClickF() => _ = PlaySfx("clickf.mp3");
        public static void PlayClickB() => _ = PlaySfx("clickb.mp3");
        public static void PlayCoin() => _ = PlaySfx("coin.mp3");
        public static void PlayGrill() => _ = PlaySfxSingle("grill.mp3");
        public static void PlayOpen() => _ = PlaySfx("open.mp3");
        public static void PlayPaper() => _ = PlaySfx("paper.mp3");
        public static void PlayHmm() => _ = PlaySfx("hmm.mp3");
        public static void PlayClose() => _ = PlaySfx("close.mp3");
        public static void PlayDelete() => _ = PlaySfx("delete.mp3");
        public static void PlayMove() => _ = PlaySfx("move.mp3");
    }
}