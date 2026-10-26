using System;
using System.IO;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerData.Test
{
    [TestFixture]
    public class FilePendingGameResultQueueTest
    {
        private string _tempDir;
        private string _queueFilePath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "BlockBlastQueueTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _queueFilePath = Path.Combine(_tempDir, "pending_results.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore transient cleanup errors in temp folder
                }
            }
        }

        [Test]
        public void Enqueue_Peek_And_Acknowledge_FollowsFifoOrder()
        {
            var queue = new FilePendingGameResultQueue(_queueFilePath, 10);

            var r1 = new GameResult("sess_1", 100, 1, 4, 1, 1000L);
            var r2 = new GameResult("sess_2", 200, 2, 8, 2, 2000L);

            Assert.AreEqual(QueueEnqueueResult.Success, queue.Enqueue(r1));
            Assert.AreEqual(QueueEnqueueResult.Success, queue.Enqueue(r2));
            Assert.AreEqual(2, queue.Count);

            Assert.IsTrue(queue.TryPeek(out var head));
            Assert.AreEqual("sess_1", head.GameSessionId);

            // Ack non-head fails
            Assert.AreEqual(QueueAcknowledgeResult.NotHead, queue.Acknowledge("sess_2"));
            Assert.AreEqual(2, queue.Count);

            // Ack head succeeds
            Assert.AreEqual(QueueAcknowledgeResult.Success, queue.Acknowledge("sess_1"));
            Assert.AreEqual(1, queue.Count);

            Assert.IsTrue(queue.TryPeek(out var nextHead));
            Assert.AreEqual("sess_2", nextHead.GameSessionId);

            Assert.AreEqual(QueueAcknowledgeResult.Success, queue.Acknowledge("sess_2"));
            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(queue.TryPeek(out _));
        }

        [Test]
        public void Enqueue_DuplicateSession_ReturnsDuplicate()
        {
            var queue = new FilePendingGameResultQueue(_queueFilePath, 10);
            var r1 = new GameResult("sess_dup", 100, 1, 4, 1, 1000L);

            Assert.AreEqual(QueueEnqueueResult.Success, queue.Enqueue(r1));
            Assert.AreEqual(QueueEnqueueResult.Duplicate, queue.Enqueue(r1));
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Enqueue_WhenCapacityReached_ReturnsFull()
        {
            var queue = new FilePendingGameResultQueue(_queueFilePath, 2);

            Assert.AreEqual(QueueEnqueueResult.Success, queue.Enqueue(new GameResult("s1", 10, 1, 1, 0, 100L)));
            Assert.AreEqual(QueueEnqueueResult.Success, queue.Enqueue(new GameResult("s2", 20, 1, 1, 0, 200L)));
            Assert.AreEqual(QueueEnqueueResult.Full, queue.Enqueue(new GameResult("s3", 30, 1, 1, 0, 300L)));

            Assert.AreEqual(2, queue.Count);
        }

        [Test]
        public void Persistence_SurvivesProcessRestart()
        {
            var queue1 = new FilePendingGameResultQueue(_queueFilePath, 10);
            queue1.Enqueue(new GameResult("s_restart", 555, 3, 10, 2, 500L));

            var queue2 = new FilePendingGameResultQueue(_queueFilePath, 10);
            Assert.AreEqual(1, queue2.Count);
            Assert.IsTrue(queue2.TryPeek(out var loaded));
            Assert.AreEqual("s_restart", loaded.GameSessionId);
            Assert.AreEqual(555, loaded.Score);
        }

        [Test]
        public void TargetCorrupted_RecoversFromBackup()
        {
            var queue = new FilePendingGameResultQueue(_queueFilePath, 10);
            queue.Enqueue(new GameResult("s1", 100, 1, 1, 0, 100L));
            queue.Enqueue(new GameResult("s2", 200, 1, 1, 0, 200L));

            // Backup exists now because of second write.
            string backupPath = _queueFilePath + ".bak";
            Assert.IsTrue(File.Exists(backupPath), "Backup file must exist after updates.");

            // Corrupt the target file
            File.WriteAllText(_queueFilePath, "CORRUPTED CONTENT {{{ not json");

            var recoveredQueue = new FilePendingGameResultQueue(_queueFilePath, 10);
            Assert.AreEqual(1, recoveredQueue.Count); // Note: s1 was in backup before s2 was committed to backup
            Assert.IsTrue(recoveredQueue.TryPeek(out var item));
            Assert.AreEqual("s1", item.GameSessionId);
        }

        [Test]
        public void BothCorrupted_ReturnsStorageCorrupt_WithoutOverwriting()
        {
            File.WriteAllText(_queueFilePath, "INVALID JSON");
            File.WriteAllText(_queueFilePath + ".bak", "INVALID BACKUP");

            var queue = new FilePendingGameResultQueue(_queueFilePath, 10);
            var res = queue.Enqueue(new GameResult("s_fail", 100, 1, 1, 0, 100L));

            Assert.AreEqual(QueueEnqueueResult.StorageCorrupt, res);
            Assert.AreEqual("INVALID JSON", File.ReadAllText(_queueFilePath), "Corrupt file must not be silently overwritten with empty queue.");
        }

        [Test]
        public void FutureSchemaVersion_ReturnsStorageCorrupt_ProtectsData()
        {
            string futureJson = "{\"schemaVersion\":999,\"results\":[]}";
            File.WriteAllText(_queueFilePath, futureJson);

            var queue = new FilePendingGameResultQueue(_queueFilePath, 10);
            var res = queue.Enqueue(new GameResult("s_future", 100, 1, 1, 0, 100L));

            Assert.AreEqual(QueueEnqueueResult.StorageCorrupt, res);
            Assert.AreEqual(futureJson, File.ReadAllText(_queueFilePath));
        }
    }
}
