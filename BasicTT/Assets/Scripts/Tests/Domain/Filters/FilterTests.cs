using NUnit.Framework;
using Domain.Filters;
using UnityEngine;

namespace Tests.Domain.Filters
{
    [TestFixture]
    public class FilterTests
    {
        [Test]
        public void PassThroughFilterVector3_ReturnsInput()
        {
            // Arrange
            var filter = new PassThroughFilterVector3();
            var input = new Vector3(1f, 2f, 3f);

            // Act
            var output = filter.Update(input);

            // Assert
            Assert.AreEqual(input, output);
        }

        [Test]
        public void MovingAverageFilterVector3_AppliesSmoothing()
        {
            // Arrange
            var filter = new MovingAverageFilterVector3(3);
            filter.Update(new Vector3(1f, 1f, 1f));
            filter.Update(new Vector3(2f, 2f, 2f));
            filter.Update(new Vector3(3f, 3f, 3f));

            // Act
            var output = filter.Update(new Vector3(4f, 4f, 4f));

            // Assert
            Assert.AreEqual(new Vector3(3f, 3f, 3f), output);
        }

        [Test]
        public void KalmanFilterVector3_StabilizesOutput()
        {
            // Arrange
            var filter = new KalmanFilterVector3();
            Vector3 input = new Vector3(5f, 5f, 5f);

            // Act
            for (int i = 0; i < 10; i++)
            {
                filter.Update(input + Random.insideUnitSphere * 0.1f);
            }
            var output = filter.Update(input);

            // Assert
            Assert.AreEqual(input.x, output.x, 0.1f);
            Assert.AreEqual(input.y, output.y, 0.1f);
            Assert.AreEqual(input.z, output.z, 0.1f);
        }
    }
}