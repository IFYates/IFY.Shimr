using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

#nullable enable
namespace Shimterface.Standard.Tests.Examples
{
    #region Production code

    /// <summary>
    /// Part of an application that wakes every interval or when needed.
    /// </summary>
    public class IntervalAction
    {
        public bool IsRunning => _runningBit > 0;
        private int _runningBit = 0;

        private ICancellationTokenSource? _tokenSource = null;

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
        [StaticShim(typeof(Thread), IsConstructor = true)]
        IThread NewThread(ThreadStart action);

        [StaticShim(typeof(CancellationTokenSource), IsConstructor = true)]
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
            inst.Start(action, TimeSpan.FromSeconds(5));
            while (inst.IsRunning) { }

            // Assert
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void Invokes_action_before_sleep()
        {
            // Arrange
            var threadMock = new Mock<IThread>();

            var threadFactoryMock = new Mock<IThreadingFactory>();
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
            threadAction?.Invoke();

            // Assert
            Assert.AreEqual(1, count);
        }
    }

    #endregion Tests
}
