using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

#nullable enable
namespace IFY.Shimr.Examples
{
    #region Production code

    /// <summary>
    /// Part of an application that wakes every interval or when needed.
    /// </summary>
    public class IntervalAction
    {
        public bool IsRunning => _runningBit > 0;
        private int _runningBit;

        private ICancellationTokenSource? _tokenSource;

        private readonly IThreadingFactory _threadingFactory;

        public IntervalAction(IThreadingFactory threadFactory)
        {
            _threadingFactory = threadFactory;
        }

        /// <summary>
        /// Start the service, if not already
        /// </summary>
        public bool Start(Action action, TimeSpan interval)
        {
            var wasRunning = Interlocked.Exchange(ref _runningBit, 1);
            if (wasRunning > 0)
            {
                return false;
            }

            _tokenSource = _threadingFactory.NewTokenSource();

            _threadingFactory.NewThread(() =>
            {
                try
                {
                    while (IsRunning)
                    {
                        action();

                        _tokenSource.Token.WaitHandle.WaitOne(interval);
                        if (_tokenSource.IsCancellationRequested)
                        {
                            _tokenSource = _threadingFactory.NewTokenSource();
                        }
                    }
                }
                finally
                {
                    _runningBit = 0;
                }
            }).Start();

            return true;
        }

        /// <summary>
        /// Stop the service without invoking again
        /// </summary>
        public void Stop()
        {
            _runningBit = 0;
            _tokenSource?.Cancel();
        }

        /// <summary>
        /// Stop sleeping and invoke again
        /// </summary>
        public void Interrupt()
        {
            _tokenSource?.Cancel();
        }
    }

    #endregion Production code

    #region Shims

    public interface IThreadingFactory
    {
        [ConstructorShim(typeof(Thread))]
        IThread NewThread(ThreadStart action);

        [ConstructorShim(typeof(CancellationTokenSource))]
        ICancellationTokenSource NewTokenSource();
    }

    public interface IThread
    {
        void Start();
    }

    public interface ICancellationTokenSource
    {
        bool IsCancellationRequested { get; }

        ICancellationToken Token { get; }

        void Cancel();
    }

    public interface ICancellationToken
    {
        IWaitHandle WaitHandle { get; }
    }

    public interface IWaitHandle
    {
        bool WaitOne(TimeSpan timeout);
    }

    #endregion Shims

    #region Tests

    [TestClass]
    public class IntervalActionExample
    {
        [TestMethod]
        public void Really_works()
        {
            // Arrange
            var threadFactory = ShimBuilder.Create<IThreadingFactory>();

            var inst = new IntervalAction(threadFactory);

            var count = 0;
            void action()
            {
                ++count;
                if (count > 2)
                {
                    inst.Stop();
                }
            }

            // Act
            inst.Start(action, TimeSpan.FromSeconds(1));
            while (inst.IsRunning)
            {
                // Wait
            }

            // Assert
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void Stop__Before_Start__Noop()
        {
            // Arrange
            var threadFactoryMock = new Mock<IThreadingFactory>();

            var inst = new IntervalAction(threadFactoryMock.Object);

            // Act
            inst.Stop();
        }

        [TestMethod]
        public void Interrupt__Before_Start__Noop()
        {
            // Arrange
            var threadFactoryMock = new Mock<IThreadingFactory>();

            var inst = new IntervalAction(threadFactoryMock.Object);

            // Act
            inst.Interrupt();
        }

        [TestMethod]
        public void Interrupt__Cancels_token()
        {
            // Arrange
            var threadFactoryMock = new Mock<IThreadingFactory>();
            var threadMock = new Mock<IThread>();
            var tokenSourceMock = new Mock<ICancellationTokenSource>();
            var tokenMock = new Mock<ICancellationToken>();

            threadFactoryMock.Setup(m => m.NewThread(It.IsAny<ThreadStart>()))
                .Returns(threadMock.Object);
            threadFactoryMock.Setup(m => m.NewTokenSource())
                .Returns(tokenSourceMock.Object);
            tokenSourceMock.SetupGet(m => m.Token)
                .Returns(tokenMock.Object);

            var inst = new IntervalAction(threadFactoryMock.Object);

            // Act
            inst.Start(() => { }, TimeSpan.MinValue);
            inst.Interrupt();

            // Assert
            tokenSourceMock.Verify(m => m.Cancel(), Times.Once);
        }

        [TestMethod]
        public void Start__Invokes_action_before_sleeping()
        {
            // Arrange
            var threadFactoryMock = new Mock<IThreadingFactory>();
            var threadMock = new Mock<IThread>();
            var tokenSourceMock = new Mock<ICancellationTokenSource>();
            var tokenMock = new Mock<ICancellationToken>();
            var waitHandleMock = new Mock<IWaitHandle>();

            var inst = new IntervalAction(threadFactoryMock.Object);

            ThreadStart? threadAction = null;
            threadFactoryMock.Setup(m => m.NewThread(It.IsAny<ThreadStart>()))
                .Returns<ThreadStart>((a) =>
                {
                    threadAction = a;
                    return threadMock.Object;
                });

            waitHandleMock.Setup(m => m.WaitOne(TimeSpan.FromSeconds(5)))
                .Returns(() =>
                {
                    inst.Stop();
                    return true;
                });
            tokenMock.SetupGet(m => m.WaitHandle)
                .Returns(waitHandleMock.Object);
            tokenSourceMock.SetupGet(m => m.IsCancellationRequested)
                .Returns(false);
            tokenSourceMock.SetupGet(m => m.Token)
                .Returns(tokenMock.Object);
            threadFactoryMock.Setup(m => m.NewTokenSource())
                .Returns(tokenSourceMock.Object);

            var count = 0;
            void action()
            {
                ++count;
            }

            // Act
            inst.Start(action, TimeSpan.FromSeconds(5));
            threadAction!.Invoke();

            // Assert
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void Start__Twice__Only_processes_once()
        {
            // Arrange
            var threadFactoryMock = new Mock<IThreadingFactory>();
            var threadMock = new Mock<IThread>();

            threadFactoryMock.Setup(m => m.NewThread(It.IsAny<ThreadStart>()))
                .Returns(threadMock.Object);

            var inst = new IntervalAction(threadFactoryMock.Object);

            // Act
            inst.Start(() => { }, TimeSpan.MinValue);
            inst.Start(() => { }, TimeSpan.MinValue);

            // Assert
            threadMock.Verify(m => m.Start(), Times.Once);
        }
    }

    #endregion Tests
}
