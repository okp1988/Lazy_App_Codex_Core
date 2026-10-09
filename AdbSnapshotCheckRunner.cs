using System.Text;

namespace Lazy_App_Codex_Core
{
    /// <summary>Opt-in, in-memory parser regression checks; never accesses ADB or app data.</summary>
    internal static class AdbSnapshotCheckRunner
    {
        public static int Run()
        {
            int cases = 0;
            int assertions = 0;
            try
            {
                void Assert(bool condition, string detail)
                {
                    assertions++;
                    if (!condition) throw new InvalidOperationException(detail);
                }

                void Check(byte[] bytes, string[] expected, int chunkSize, string name)
                {
                    cases++;
                    var snapshots = new List<string>();
                    var reader = new AdbDeviceSnapshotReader(snapshots.Add);
                    for (int offset = 0; offset < bytes.Length; offset += chunkSize)
                    {
                        reader.Feed(bytes.AsSpan(offset, Math.Min(chunkSize, bytes.Length - offset)));
                        Assert(snapshots.Count <= expected.Length, name + ": unexpected extra snapshot");
                        for (int index = 0; index < snapshots.Count; index++)
                        {
                            Assert(snapshots[index] == expected[index], name + ": partial or incorrect snapshot");
                        }
                    }

                    reader.Complete();
                    reader.Complete();
                    Assert(snapshots.SequenceEqual(expected), name + ": final snapshots differ");
                }

                void CheckChunkSizes(byte[] bytes, string[] expected, string name)
                {
                    foreach (int size in new[] { 1, 2, 3, 4, 7, 17, 64, Math.Max(1, bytes.Length) }.Distinct())
                    {
                        Check(bytes, expected, size, name + " chunk=" + size);
                    }
                }

                const string one = "X13\tdevice\n";
                const string two = "X13\tdevice\nS26\tdevice\n";
                const string many = "X13\tdevice\nS26\tdevice\nthird\toffline\nfourth\tunauthorized\nfifth\tdevice\n";
                // Synthetic serials preserve the captured 109 logical / 111 raw byte shape.
                string capturedShape = new string('x', 47) + "\tdevice\n" + new string('s', 46) + "\tdevice\n";
                byte[] capturedFrame = Frame(capturedShape, crlf: true);
                Assert(Encoding.ASCII.GetString(capturedFrame, 0, 4) == "006d", "Captured shape length header");
                Assert(capturedFrame.Length == 115, "Captured shape raw byte count");
                CheckChunkSizes(capturedFrame, new[] { capturedShape }, "Captured two-row CRLF shape");

                var sequence = new[] { "", one, two, many, one, "" };
                CheckChunkSizes(Frames(sequence, crlf: true), sequence, "CRLF device join/loss/empty");
                CheckChunkSizes(Frames(sequence, crlf: false), sequence, "LF device join/loss/empty");
                CheckChunkSizes(Frame(one, false).Concat(Frame(many, true)).Concat(Frame("", false)).ToArray(),
                    new[] { one, many, "" }, "Mixed frame line endings");
                const string unicode = "设备一\tdevice\n设备二\toffline\n";
                CheckChunkSizes(Frame(unicode, true), new[] { unicode }, "UTF8 byte counts");

                // Every possible two-chunk boundary, including between the last CR and LF.
                for (int split = 0; split <= capturedFrame.Length; split++)
                {
                    cases++;
                    var snapshots = new List<string>();
                    var reader = new AdbDeviceSnapshotReader(snapshots.Add);
                    reader.Feed(capturedFrame.AsSpan(0, split));
                    Assert(snapshots.Count == (split == capturedFrame.Length ? 1 : 0),
                        "Captured split=" + split + ": frame published too early");
                    reader.Feed(capturedFrame.AsSpan(split));
                    reader.Complete();
                    Assert(snapshots.SequenceEqual(new[] { capturedShape }), "Captured split=" + split);
                }

                // Device status stays complete: joining S26 keeps ready X13 in the same snapshot.
                cases++;
                var readySnapshots = new List<string>();
                new AdbDeviceSnapshotReader(readySnapshots.Add).Feed(Frames(new[] { one, two, many }, true));
                var status = AdbShellController.BuildDeviceStatus(AdbShellController.ParseDeviceLines(readySnapshots[1]));
                Assert(status.DeviceCount == 2 && status.Devices.Any(d => d.Serial == "X13" && d.IsReady) &&
                    status.Devices.Any(d => d.Serial == "S26" && d.IsReady), "Device join preserves both ready serials");
                var manyStatus = AdbShellController.BuildDeviceStatus(AdbShellController.ParseDeviceLines(readySnapshots[2]));
                Assert(manyStatus.Devices.Count == 5 && manyStatus.DeviceCount == 3 &&
                    manyStatus.Devices.Last().State == "device", "Multi-row final state must not be truncated");

                CheckChunkSizes(Encoding.UTF8.GetBytes("List of devices attached\r\nX13\tdevice\r\nS26\toffline\r\n\r\n"),
                    new[] { "X13\tdevice\nS26\toffline" }, "Plain CRLF mode");
                CheckChunkSizes(Encoding.UTF8.GetBytes("ABCD\tdevice\n\n"), new[] { "ABCD\tdevice" }, "Plain hex serial");
                CheckChunkSizes(Encoding.UTF8.GetBytes("List of devices attached\r\n\r\n"), new[] { "" }, "Plain empty list");
                CheckChunkSizes(Encoding.UTF8.GetBytes("X13\tdevice\n"), new[] { "X13\tdevice" }, "Plain EOF final row");

                Check(Encoding.ASCII.GetBytes("00"), Array.Empty<string>(), 1, "Truncated header");
                Check(capturedFrame[..^1], Array.Empty<string>(), 1, "Truncated final CRLF");
                Check(Frame(many, true)[..^5], Array.Empty<string>(), 3, "Truncated payload");
                Check(Encoding.ASCII.GetBytes("X13\tdevice"), Array.Empty<string>(), 2, "Unterminated plain row");

                cases++;
                bool invalidPrefixRejected = false;
                var validBeforeFailure = new List<string>();
                try
                {
                    new AdbDeviceSnapshotReader(validBeforeFailure.Add).Feed(Encoding.ASCII.GetBytes("0000ZZZZ"));
                }
                catch (InvalidDataException) { invalidPrefixRejected = true; }
                Assert(invalidPrefixRejected && validBeforeFailure.SequenceEqual(new[] { "" }), "Malformed header still rejected");

                cases++;
                bool invalidUtf8Rejected = false;
                try
                {
                    new AdbDeviceSnapshotReader(_ => { }).Feed(new byte[] { 48, 48, 48, 50, 0xc3, 0x28 });
                }
                catch (DecoderFallbackException) { invalidUtf8Rejected = true; }
                Assert(invalidUtf8Rejected, "Malformed UTF8 still rejected");

                cases++;
                var completedReader = new AdbDeviceSnapshotReader(_ => { });
                completedReader.Complete();
                bool afterCompleteRejected = false;
                try { completedReader.Feed(new byte[] { 48 }); }
                catch (InvalidOperationException) { afterCompleteRejected = true; }
                Assert(afterCompleteRejected, "Feed after Complete still rejected");

                Console.WriteLine($"ADB snapshot checks PASS: {cases} cases, {assertions} assertions. No ADB/UI/config/log access.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ADB snapshot checks FAIL after {cases} cases / {assertions} assertions: {ex.Message}");
                return 1;
            }
        }

        private static byte[] Frame(string logicalPayload, bool crlf)
        {
            int length = Encoding.UTF8.GetByteCount(logicalPayload);
            string rawPayload = crlf ? logicalPayload.Replace("\n", "\r\n") : logicalPayload;
            return Encoding.UTF8.GetBytes(length.ToString("x4") + rawPayload);
        }

        private static byte[] Frames(IEnumerable<string> payloads, bool crlf) =>
            payloads.SelectMany(payload => Frame(payload, crlf)).ToArray();
    }
}
