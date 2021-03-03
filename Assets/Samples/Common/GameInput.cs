using Samples.MyGameLib.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Samples.Common
{
    public class GameInput : PlayerInput.IPlayerActions
    {
        private PlayerInput m_PlayerInput;
        private Vector2 m_CharacterMovement;
        private Vector2 _mousePos;
        private bool m_Fire;
        private bool m_Speed;
        private bool _jump;
        private bool mouseRight;
        private bool r, t, g;

        /// <summary>
        /// pitch围绕x轴旋转
        /// yaw围绕y轴旋转
        /// 客户端的旋转直接发给服务端
        /// </summary>
        private float pitch, yaw;

        private float _deltaTime;

        public GameInput()
        {
            m_PlayerInput = new PlayerInput();
            m_PlayerInput.Player.SetCallbacks(this);
            m_PlayerInput.Enable();
        }

        public void Update(float dt)
        {
            _deltaTime = dt;
            InputSystem.Update();
        }

        public InputCommand GetInputCommand()
        {
            var rot = _mousePos * _deltaTime * 2.5f;
            pitch -= rot.y;
            yaw += rot.x;

            var input = new InputCommand
            {
                Movement = m_CharacterMovement,
                Jump = _jump,
                Fire = m_Fire,
                Speed = m_Speed,
                MouseRight = mouseRight,
                R = r,
                T = t,
                G = g,
                Yaw = yaw,
                Pitch = pitch
            };

            return input;
        }

        public void OnMove(InputAction.CallbackContext context) =>
            m_CharacterMovement = context.ReadValue<Vector2>();

        public void OnLook(InputAction.CallbackContext context)
        {
            _mousePos = context.ReadValue<Vector2>();
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                m_Fire = true;
            }

            if (context.canceled)
            {
                m_Fire = false;
            }
        }

        public void OnSpeed(InputAction.CallbackContext context)
        {
            if (context.started)
                m_Speed = true;
            if (context.canceled)
                m_Speed = false;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            _jump = context.ReadValueAsButton();
        }

        public void OnMoseRight(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                mouseRight = true;
            }

            if (context.canceled)
            {
                mouseRight = false;
            }
        }

        public void OnR(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                r = true;
            }

            if (context.canceled)
            {
                r = false;
            }
        }

        public void OnT(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                t = true;
            }

            if (context.canceled)
            {
                t = false;
            }
        }

        public void OnG(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                g = true;
            }

            if (context.canceled)
            {
                g = false;
            }
        }
    }
}