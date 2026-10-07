using System.Text;

namespace Lazy_App_Codex_Core
{
    /// <summary>Publishes complete device lists from the ADB track-devices output stream.</summary>
    public sealed class AdbDeviceSnapshotReader
    {
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private readonly Action<string> _onSnapshot;
        private readonly List<byte> _buffer = new();
        private readonly List<string> _plainRows = new();
        private OutputMode _mode;
        private bool _plainSnapshotStarted;
        private bool _completed;

        public AdbDeviceSnapshotReader(Action<string> onSnapshot)
        {
            _onSnapshot = onSnapshot ?? throw new ArgumentNullException(nameof(onSnapshot));
        }

        public void Feed(ReadOnlySpan<byte> bytes)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The device output stream is already complete.");
            }

            foreach (byte value in bytes)
            {
                _buffer.Add(value);
            }

            DetectMode();
            if (_mode == OutputMode.Framed)
            {
                ReadFrames();
            }
            else if (_mode == OutputMode.Plain)
            {
                ReadPlainLines();
            }
        }

        public void Complete()
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            // A truncated frame or unterminated row is not a trustworthy snapshot.
            // Only the complete newline-terminated rows can form the final plain list.
            if (_mode == OutputMode.Plain && _buffer.Count == 0 && _plainSnapshotStarted)
            {
                PublishPlainSnapshot();
            }

            _buffer.Clear();
            _plainRows.Clear();
        }

        private void DetectMode()
        {
            if (_mode != OutputMode.Unknown)
            {
                return;
            }

            int prefixBytes = Math.Min(4, _buffer.Count);
            for (int index = 0; index < prefixBytes; index++)
            {
                if (HexDigit(_buffer[index]) < 0)
                {
                    _mode = OutputMode.Plain;
                    return;
                }
            }

            if (_buffer.Count < 4)
            {
                return;
            }

            int length = ReadLength();
            if (length == 0)
            {
                _mode = OutputMode.Framed;
            }
            else if (_buffer.Count > 4)
            {
                // A four-character hexadecimal serial followed by a separator is
                // a plain row, not a length prefix. Plain mode never redetects frames.
                _mode = _buffer[4] is (byte)'\t' or (byte)' ' or (byte)'\r' or (byte)'\n'
                    ? OutputMode.Plain
                    : OutputMode.Framed;
            }
        }

        private void ReadFrames()
        {
            while (_buffer.Count >= 4)
            {
                int length = ReadLength();
                if (_buffer.Count < 4 + length)
                {
                    return;
                }

                string snapshot = Utf8.GetString(_buffer.GetRange(4, length).ToArray());
                _buffer.RemoveRange(0, 4 + length);
                _onSnapshot(snapshot);
            }
        }

        private int ReadLength()
        {
            int length = 0;
            for (int index = 0; index < 4; index++)
            {
                int digit = HexDigit(_buffer[index]);
                if (digit < 0)
                {
                    throw new InvalidDataException("ADB device snapshot has an invalid length prefix.");
                }

                length = length * 16 + digit;
            }

            return length;
        }

        private static int HexDigit(byte value)
        {
            return value switch
            {
                >= (byte)'0' and <= (byte)'9' => value - '0',
                >= (byte)'a' and <= (byte)'f' => value - 'a' + 10,
                >= (byte)'A' and <= (byte)'F' => value - 'A' + 10,
                _ => -1
            };
        }

        private void ReadPlainLines()
        {
            int newline;
            while ((newline = _buffer.IndexOf((byte)'\n')) >= 0)
            {
                string line = Utf8.GetString(_buffer.GetRange(0, newline).ToArray()).TrimEnd('\r');
                _buffer.RemoveRange(0, newline + 1);
                if (string.IsNullOrWhiteSpace(line))
                {
                    PublishPlainSnapshot();
                }
                else if (line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
                {
                    if (_plainSnapshotStarted)
                    {
                        PublishPlainSnapshot();
                    }

                    _plainSnapshotStarted = true;
                }
                else if (line.Contains('\t'))
                {
                    _plainRows.Add(line);
                    _plainSnapshotStarted = true;
                }
            }
        }

        private void PublishPlainSnapshot()
        {
            _onSnapshot(string.Join('\n', _plainRows));
            _plainRows.Clear();
            _plainSnapshotStarted = false;
        }

        private enum OutputMode
        {
            Unknown,
            Framed,
            Plain
        }
    }
}
