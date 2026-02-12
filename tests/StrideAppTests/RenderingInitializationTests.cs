using FluentAssertions;
using Game.Core.Testing;
using Xunit;

namespace Game.StrideApp.Tests;

/// <summary>
/// Platform-agnostic tests for rendering system initialization.
/// These tests use ONLY the core ITestBridge interface and can run on any platform.
/// </summary>
public class RenderingInitializationTests
{
    /// <summary>
    /// Tests using InMemoryTestBridge - works without Stride runtime.
    /// </summary>
    public class InMemoryTests
    {
        [Fact]
        public void RenderingSystem_ShouldBeInitialized_AfterInitializeCommand()
        {
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 1);
            scenario.Step();

            // Assert - Using core fluent API
            scenario.Assert.Rendering()
                .IsInitialized()
                .HasCameraSlots(1);
        }

        [Fact]
        public void RenderingSystem_ShouldHaveMultipleCameraSlots_WhenRequested()
        {
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 3);

            // Assert
            scenario.Assert.Rendering()
                .IsInitialized()
                .HasCameraSlots(3);
        }

        [Fact]
        public void Camera_ShouldBeCreatable_AfterRenderingInitialized()
        {
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 1);
            var camera = scenario.Camera("MainCamera", x: 0, y: 10, z: 20);
            camera.SetAsActiveCamera();
            scenario.Step();

            // Assert
            scenario.Assert.Entity(camera)
                .Exists()
                .IsOfType("Camera")
                .HasPosition(0, 10, 20);

            scenario.Assert.Rendering()
                .HasActiveCamera();
        }

        [Fact]
        public void Entity_ShouldBeCreatable_InScene()
        {
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering();
            var model = scenario.Entity("TestModel", "Model", x: 5, y: 0, z: 10);
            scenario.Step();

            // Assert
            scenario.Assert.Entity(model)
                .Exists()
                .IsOfType("Model")
                .HasPosition(5, 0, 10);
        }

        [Fact]
        public void RenderingSystem_ShouldNotHaveActiveCamera_BeforeSet()
        {
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 1);
            scenario.Camera("MainCamera");
            // Note: NOT calling SetAsActiveCamera

            // Assert
            var snapshot = scenario.GetSnapshot();
            snapshot.Rendering.ActiveCameraId.Should().BeNull();
        }

        [Fact]
        public void InitializationOrder_ShouldAllowCameraCreation_AfterRenderingInit()
        {
            // This test verifies the fix for the original bug:
            // Rendering must be initialized BEFORE accessing camera slots
            
            // Arrange
            var bridge = new InMemoryTestBridge();
            var scenario = new TestScenario(bridge);

            // Act - Correct order
            scenario.InitializeRendering(cameraSlots: 1);
            var camera = scenario.Camera("Camera");
            camera.SetAsActiveCamera(slotIndex: 0);

            // Assert - No exception, everything works
            scenario.Assert.Rendering()
                .IsInitialized()
                .HasCameraSlots(1)
                .HasActiveCamera();
        }
    }

    /// <summary>
    /// Tests that can be parameterized to run on multiple bridges.
    /// This demonstrates dual-targeting capability.
    /// </summary>
    public class CrossPlatformTests
    {
        public static IEnumerable<object[]> TestBridges()
        {
            yield return new object[] { new InMemoryTestBridge() };
            // Future: yield return new object[] { new BrowserTestBridge() };
            // Future: yield return new object[] { new StrideTestBridge(game) };
        }

        [Theory]
        [MemberData(nameof(TestBridges))]
        public void RenderingInitialization_ShouldWork_OnAllPlatforms(ITestBridge bridge)
        {
            // Arrange
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering(cameraSlots: 1);

            // Assert
            scenario.Assert.Rendering()
                .IsInitialized()
                .HasCameraSlots(1);
        }

        [Theory]
        [MemberData(nameof(TestBridges))]
        public void CameraCreation_ShouldWork_OnAllPlatforms(ITestBridge bridge)
        {
            // Arrange
            var scenario = new TestScenario(bridge);

            // Act
            scenario.InitializeRendering();
            var camera = scenario.Camera("TestCamera", x: 0, y: 5, z: 10);
            camera.SetAsActiveCamera();

            // Assert
            scenario.Assert.Entity(camera).Exists().IsOfType("Camera");
            scenario.Assert.Rendering().HasActiveCamera();
        }
    }
}
