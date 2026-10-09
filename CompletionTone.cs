using System.Media;
using System.Text;

namespace Lazy_App_Codex_Core
{
    /// <summary>One short, high-amplitude completion tone without external audio files.</summary>
    internal sealed class CompletionTone : IDisposable
    {
        private readonly MemoryStream _wave = new(CreateWave(), writable: false);
        private readonly SoundPlayer _player;

        public CompletionTone()
        {
            _player = new SoundPlayer(_wave);
            try
            {
                _player.Load();
            }
            catch
            {
                _player.Dispose();
                _wave.Dispose();
                throw;
            }
        }

        public void Play() => _player.Play();

        public void Dispose()
        {
            _player.Stop();
            _player.Dispose();
            _wave.Dispose();
        }

        private static byte[] CreateWave()
        {
            const int sampleRate = 44100;
            const int sampleCount = sampleRate * 300 / 1000;
            const int dataSize = sampleCount * sizeof(short);
            const int fadeSamples = sampleRate * 5 / 1000;
            using var stream = new MemoryStream(44 + dataSize);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write("RIFF"u8);
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)1); // Mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(dataSize);
            for (int index = 0; index < sampleCount; index++)
            {
                // Short fades avoid clicks; 90% peak amplitude leaves headroom.
                double fade = Math.Min(1.0, Math.Min(index, sampleCount - 1 - index) / (double)fadeSamples);
                double sample = Math.Sin(2 * Math.PI * 1000 * index / sampleRate) * short.MaxValue * 0.9 * fade;
                writer.Write((short)Math.Round(sample));
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
