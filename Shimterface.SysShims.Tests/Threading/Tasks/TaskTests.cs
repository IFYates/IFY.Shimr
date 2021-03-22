using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Shimterface.SysShims.Threading.Tasks.Tests
{
	[TestClass]
	public class TaskTests
	{
		public void ContinueWith__Logic(ITaskFactory factory, out bool taskRan, out bool taskContinued)
		{
			var hasRun = false;
			var task = factory.Create(() => hasRun = true);

			var hasContinued = false;
			var task2 = task.ContinueWith(t => hasContinued = true, TaskContinuationOptions.ExecuteSynchronously);

			task.Start();
			task2.Wait(); // Docs say task.Wait() should be enough with ExecuteSynchronously, but it isn't (unrelated to shimming)
			
			taskRan = hasRun;
			taskContinued = hasContinued;
		}
		[TestMethod]
		public void ContinueWith__Real()
		{
			// Arrange
			var factory = ShimBuilder.Create<ITaskFactory>();

			// Act
			ContinueWith__Logic(factory, out var taskRan, out var taskContinued);

			// Assert
			Assert.IsTrue(taskRan);
			Assert.IsTrue(taskContinued);
		}
		[TestMethod]
		public void ContinueWith__Shim()
		{
			// Arrange
			Action taskAction = null;
			Action taskContinue = null;

			var taskMock = new Mock<ITask>(MockBehavior.Strict);
			taskMock.Setup(m => m.ContinueWith(It.IsAny<Action<Task>>(), It.IsAny<TaskContinuationOptions>()))
				.Returns<Action<Task>, TaskContinuationOptions>((a, o) =>
				{
					taskContinue = () => a(null);
					return taskMock.Object;
				});
			taskMock.Setup(m => m.Start())
				.Callback(() => taskAction());
			taskMock.Setup(m => m.Wait())
				.Callback(() => taskContinue());

			var factoryMock = new Mock<ITaskFactory>(MockBehavior.Strict);
			factoryMock.Setup(m => m.Create(It.IsAny<Action>()))
				.Returns<Action>((a) =>
				{
					taskAction = a;
					return taskMock.Object;
				});

			// Act
			ContinueWith__Logic(factoryMock.Object, out var taskRan, out var taskContinued);

			// Assert
			taskMock.VerifyAll();
			factoryMock.VerifyAll();
			Assert.IsTrue(taskRan);
			Assert.IsTrue(taskContinued);
		}
	}
}
