using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.ADT.Administration.Logs;
using Content.Server.Database;
using Content.Shared.Database;
using NUnit.Framework;

namespace Content.Tests.Server.ADT
{
    [TestFixture]
    public sealed class AdminLogChunkTest
    {
        private static List<AdminLog> MakeLogs(int count)
        {
            var players = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
            };

            var date = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);
            var logs = new List<AdminLog>(count);

            for (var i = 0; i < count; i++)
            {
                var log = new AdminLog
                {
                    Id = i + 1,
                    RoundId = 7,
                    Type = (LogType) (i % 40),
                    Impact = (LogImpact) ((i % 4) - 1),
                    Date = date.AddMilliseconds(i * 137),
                    Message = $"Лог номер {i}: player attacked something with id {i * 31}",
                    Players = new List<AdminLogPlayer>(),
                };

                for (var p = 0; p <= i % 3; p++)
                {
                    log.Players.Add(new AdminLogPlayer
                    {
                        RoundId = 7,
                        LogId = log.Id,
                        PlayerUserId = players[(i + p) % players.Length],
                    });
                }

                logs.Add(log);
            }

            return logs;
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(500)]
        public void ChunkRoundTrip(int count)
        {
            var logs = MakeLogs(count);

            var builder = new AdtLogChunkBuilder();
            builder.Reset(7, 0);

            foreach (var log in logs)
            {
                builder.Append(log);
            }

            var entity = builder.Finish(6);
            var header = AdtLogChunkHeader.FromEntity(entity);

            Assert.Multiple(() =>
            {
                Assert.That(header.LogCount, Is.EqualTo(count));
                Assert.That(header.FirstLogId, Is.EqualTo(logs[0].Id));
                Assert.That(header.LastLogId, Is.EqualTo(logs[^1].Id));
                Assert.That(header.FirstDate, Is.EqualTo(logs[0].Date));
                Assert.That(header.LastDate, Is.EqualTo(logs[^1].Date));
                if (count >= 100)
                    Assert.That(entity.Payload.Length, Is.LessThan(header.RawSize / 2));
            });

            var raw = new byte[header.RawSize];
            var size = AdtLogChunkFormat.Decompress(entity.Payload, raw);

            Assert.That(size, Is.EqualTo(header.RawSize));

            var indices = new int[Math.Max(1, header.Players.Length)];
            var scanner = new AdtLogChunkScanner(raw.AsSpan(0, size), indices);

            Assert.That(scanner.Count, Is.EqualTo(count));

            var read = 0;

            while (scanner.MoveNext())
            {
                var expected = logs[read];

                Assert.That(scanner.Id, Is.EqualTo(expected.Id));
                Assert.That(scanner.Type, Is.EqualTo(expected.Type));
                Assert.That(scanner.Impact, Is.EqualTo(expected.Impact));
                Assert.That(AdtLogChunkFormat.FromUnixMs(scanner.DateMs), Is.EqualTo(expected.Date));
                Assert.That(Encoding.UTF8.GetString(scanner.Message), Is.EqualTo(expected.Message));
                Assert.That(scanner.PlayerCount, Is.EqualTo(expected.Players.Count));

                for (var i = 0; i < scanner.PlayerCount; i++)
                {
                    Assert.That(header.Players[scanner.Players[i]], Is.EqualTo(expected.Players[i].PlayerUserId));
                }

                read++;
            }

            Assert.That(read, Is.EqualTo(count));
        }

        [Test]
        public void HeaderMasksCoverContent()
        {
            var logs = MakeLogs(64);

            var builder = new AdtLogChunkBuilder();
            builder.Reset(7, 0);

            foreach (var log in logs)
            {
                builder.Append(log);
            }

            var header = AdtLogChunkHeader.FromEntity(builder.Finish(6));

            foreach (var log in logs)
            {
                Assert.That(header.HasType(log.Type), Is.True, $"Тип {log.Type} потерян в маске.");
                Assert.That(header.HasImpact(log.Impact), Is.True, $"Impact {log.Impact} потерян в маске.");
            }

            Assert.That(header.HasType(LogType.Connection), Is.False);
            Assert.That(header.HasNonPlayerLogs, Is.False);
        }

        [Test]
        public void EmptyPlayersSetFlag()
        {
            var logs = MakeLogs(4);

            foreach (var log in logs)
            {
                log.Players.Clear();
            }

            var builder = new AdtLogChunkBuilder();
            builder.Reset(7, 0);

            foreach (var log in logs)
            {
                builder.Append(log);
            }

            var entity = builder.Finish(6);
            var header = AdtLogChunkHeader.FromEntity(entity);

            Assert.Multiple(() =>
            {
                Assert.That(header.HasNonPlayerLogs, Is.True);
                Assert.That(header.Players, Is.Empty);
            });
        }
    }
}
