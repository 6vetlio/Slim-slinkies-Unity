using System;
using UnityEngine;

namespace _Scripts
{
    public class NoahsParallax : MonoBehaviour
    {
        [SerializeField] private float AmountOfParallax = 1f;
        [SerializeField] private TrainMover trainMover;
        [SerializeField] private bool moveOnlyWhileTravelling = true;
        [Tooltip("Idle scroll multiplier applied to AmountOfParallax when moveOnlyWhileTravelling is off. 0 = no idle drift.")]
        [SerializeField] private float idleSpeedMultiplier = 0f;
        [Tooltip("World units the layer scrolls per (km/h) per second. 3.0 → 80 km/h = 240 u/s on a 1.0-amount layer; 700 km/h = 2100 u/s. Bump higher if it still feels slow.")]
        [SerializeField] private float speedKmhToWorldUnitsPerSecond = 3.0f;

        private float _startingPos;
        private float _lengthOfSprite;

        private void Start()
        {
            _startingPos = transform.position.x;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                _lengthOfSprite = spriteRenderer.bounds.size.x;
            }

            if (trainMover == null)
            {
                trainMover = FindFirstObjectByType<TrainMover>();
            }

            // Brutus needs to see at a glance whether the scroll knob is healthy
            // when nothing visibly moves. AmountOfParallax × knob × current top
            // speed = world units/second this layer scrolls during travel.
            float topSpeedKmh = GameManager.Instance != null ? GameManager.Instance.CurrentTrainTopSpeedKmh : 80f;
            float unitsPerSecond = Mathf.Max(0f, AmountOfParallax) * Mathf.Max(0f, speedKmhToWorldUnitsPerSecond) * topSpeedKmh;
            Debug.Log("[NoahsParallax] " + name + " — AmountOfParallax=" + AmountOfParallax + " × kmh-knob=" + speedKmhToWorldUnitsPerSecond + " × topSpeed=" + topSpeedKmh + " → " + unitsPerSecond + " units/sec while travelling");
        }

        private void Update()
        {
            float speed = GetScrollSpeed();
            if (Mathf.Approximately(speed, 0f))
            {
                return;
            }

            _startingPos -= speed * Time.deltaTime;

            Vector3 newPosition = transform.position;
            newPosition.x = _startingPos;
            transform.position = newPosition;

            if (_lengthOfSprite <= 0f)
            {
                return;
            }

            float wrapLimit = -_lengthOfSprite;
            if (transform.position.x <= wrapLimit)
            {
                _startingPos += _lengthOfSprite * 2f;
                newPosition.x = _startingPos;
                transform.position = newPosition;
            }
        }

        private float GetScrollSpeed()
        {
            bool isTravelling = trainMover != null && trainMover.MovementStatus == TrainMovementStatus.Travelling;
            if (!isTravelling && moveOnlyWhileTravelling)
            {
                return 0f;
            }

            // Drive the world directly off the current tier's top speed so the
            // parallax actually matches the speedometer. Each layer scales by its
            // own AmountOfParallax — distant layers tiny, foreground big.
            float topSpeedKmh = GameManager.Instance != null
                ? GameManager.Instance.CurrentTrainTopSpeedKmh
                : 80f;

            float travelUnitsPerSecond = topSpeedKmh * Mathf.Max(0f, speedKmhToWorldUnitsPerSecond);
            float stateMultiplier = isTravelling ? travelUnitsPerSecond : idleSpeedMultiplier;

            return Mathf.Max(0f, AmountOfParallax) * stateMultiplier;
        }
    }
}
