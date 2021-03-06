using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovementKB : MonoBehaviour
{
    private CharacterController _controller;
    [SerializeField] private Transform _cam = null;
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _turnSmoothTime = 0.1f;
    private float _turnSmoothVelocity;

    private Vector3 _moveDirection;
    private Vector3 _moveVelocity;
    private Vector3 _prevMoveDirection;
    private Vector3 _prevMoveVelocity;
    public Vector3 MoveDirection1 { get { return _prevMoveDirection; } }
    public Vector3 MoveDirection2 { get { return _moveDirection; } }
    public Vector3 MoveVelocity1 { get { return _prevMoveVelocity; } }
    public Vector3 MoveVelocity2 { get { return _moveVelocity; } }

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1.0f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1.0f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1.0f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1.0f;
        }
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Pre-update the values from previous frame
            _prevMoveDirection = _moveDirection;
            _prevMoveVelocity = _moveVelocity;

            _moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _moveVelocity = _moveDirection.normalized * _speed * Time.deltaTime;
            _controller.Move(_moveVelocity);
        }
        else
        {
            _moveDirection = Vector3.zero;
            _moveVelocity = Vector3.zero;
        }
    }
}
