using Godot;
using System;
// Simple Character Controller for Demonstration Purposes
public partial class DemoCharacterController : CharacterBody3D
{
    private Camera3D camera;
    private SpotLight3D light;

    private Vector2 mouseDelta;

    public bool Enabled;
    public override void _Ready()
    {
        Enabled = false;
        InputMap.AddAction("DemoCharacter_Forward");
        InputMap.ActionAddEvent("DemoCharacter_Forward", new InputEventKey() { Keycode = Key.W });

        InputMap.AddAction("DemoCharacter_Backward");
        InputMap.ActionAddEvent("DemoCharacter_Backward", new InputEventKey() { Keycode = Key.S });

        InputMap.AddAction("DemoCharacter_Left");
        InputMap.ActionAddEvent("DemoCharacter_Left", new InputEventKey() { Keycode = Key.A });

        InputMap.AddAction("DemoCharacter_Right");
        InputMap.ActionAddEvent("DemoCharacter_Right", new InputEventKey() { Keycode = Key.D });

        camera = (Camera3D)FindChild("Camera3D");
        light = (SpotLight3D)FindChild("SpotLight3D");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            mouseDelta = ((InputEventMouseMotion)@event).Relative * 0.01f;
        }
        if (@event is InputEventMouseButton && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            if (((InputEventMouseButton)@event).ButtonIndex == MouseButton.Left && ((InputEventMouseButton)@event).Pressed)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
        if (@event is InputEventKey && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (((InputEventKey)@event).IsPressed() && ((InputEventKey)@event).Keycode == Key.Escape)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Enabled) { return; }

        camera.Rotation = camera.Rotation with { X = Math.Clamp(camera.Rotation.X - mouseDelta.Y, -1.3f, 1.3f), Y = (camera.Rotation.Y - mouseDelta.X) };
        mouseDelta = Vector2.Zero;

        Vector2 input = Input.GetVector("DemoCharacter_Left", "DemoCharacter_Right", "DemoCharacter_Backward", "DemoCharacter_Forward");

        Vector3 movement = (camera.GlobalBasis.X with { X = camera.GlobalBasis.X.X, Z = camera.GlobalBasis.X.Z }) * input.X + (camera.GlobalBasis.X with { X = camera.GlobalBasis.X.Z, Z = -camera.GlobalBasis.X.X }) * input.Y;
        Vector3 velocity = Velocity.MoveToward((movement * 3.0f), (float)delta * 10.0f) * new Vector3(1, 0, 1);

        Velocity = velocity + Vector3.Up * (Velocity.Y - 9.81f * (float)delta);

        light.Rotation = light.Rotation.Lerp(camera.Rotation, 0.2f);

        MoveAndSlide();
    }
}
