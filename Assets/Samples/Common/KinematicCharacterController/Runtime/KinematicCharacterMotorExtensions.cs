namespace KinematicCharacterController
{
    public static class KinematicCharacterMotorExtensions
    {
        public static void Simulate(this KinematicCharacterMotor motor, float deltaTime)
        {
            motor.InitialTickPosition = motor.TransientPosition;
            motor.InitialTickRotation = motor.TransientRotation;

            motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);

            motor.UpdatePhase1(deltaTime);
            motor.UpdatePhase2(deltaTime);

            motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);

            //motor.Transform.Translate(motor.GetComponent<CharacterControl>()._moveDir * deltaTime);
        }
    }
}