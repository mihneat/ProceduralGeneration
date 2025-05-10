using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10;
        [SerializeField] private float sprintSpeed = 25;
        [SerializeField] private float rotationSpeed = 2;
        [SerializeField] private Transform verticalRotationPoint;

        private Vector2 _currMoveDirection;
        private bool _isSprinting;
        private bool _shouldJump;
    
        private Rigidbody rb;
    
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_currMoveDirection.sqrMagnitude < 0.1f)
                return;

            float speed = _isSprinting ? sprintSpeed : moveSpeed;
            Vector3 moveDir = transform.forward * _currMoveDirection.y + transform.right * _currMoveDirection.x; 
            rb.MovePosition(transform.position + moveDir * (speed * Time.deltaTime));
        }

        public void Move(InputAction.CallbackContext ctx)
        {
            _currMoveDirection = ctx.ReadValue<Vector2>().normalized;
        }
    
        public void Look(InputAction.CallbackContext ctx)
        {
            Vector2 lookDir = ctx.ReadValue<Vector2>();
        
            // Rotate the player horizontally
            transform.Rotate(Vector3.up, lookDir.x * rotationSpeed * Time.deltaTime, Space.World);
            verticalRotationPoint.Rotate(Vector3.left, lookDir.y * rotationSpeed * Time.deltaTime, Space.Self);

            // // TODO: Bad but it kinda works
            // float currVerticalRotation = verticalRotationPoint.localRotation.eulerAngles.x;
            // if (currVerticalRotation < 0.0f)
            //     verticalRotationPoint.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            // else if (currVerticalRotation > 359.0f)
            //     verticalRotationPoint.localRotation = Quaternion.Euler(359.0f, 0.0f, 0.0f);
            //
            // if (currVerticalRotation > 180.0f && currVerticalRotation < 275.0f)
            //     verticalRotationPoint.localRotation = Quaternion.Euler(275.0f, 0.0f, 0.0f);
            // else if (currVerticalRotation < 180.0f && currVerticalRotation > 85.0f)
            //     verticalRotationPoint.localRotation = Quaternion.Euler( 85.0f, 0.0f, 0.0f);
        }

        public void Sprint(InputAction.CallbackContext ctx)
        {
            _isSprinting = ctx.ReadValueAsButton();
        }

        public void Jump(InputAction.CallbackContext ctx)
        {
            _shouldJump = ctx.ReadValueAsButton();
        }
    }
}
