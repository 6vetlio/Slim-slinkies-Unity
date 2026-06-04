using System;
using UnityEngine;

namespace _Scripts
{
    public class NoahsParallax : MonoBehaviour
    {
        [SerializeField] private float AmountOfParallax = 1f;
        [SerializeField] private TrainMover trainMover;
        [SerializeField] private TransportSwitcher transportSwitcher;
        [SerializeField] private bool moveOnlyWhileTravelling = true;
        [SerializeField] private float idleSpeedMultiplier = 0f;
        [SerializeField] private float travelSpeedMultiplier = 60f;
        [SerializeField] private float hyperloopSpeedMultiplier = 300f;

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

            if (transportSwitcher == null)
            {
                transportSwitcher = FindFirstObjectByType<TransportSwitcher>();
            }
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

            float travelMultiplier = travelSpeedMultiplier;
            if (isTravelling && transportSwitcher != null && transportSwitcher.IsHyperloopActive())
            {
                travelMultiplier = hyperloopSpeedMultiplier;
            }

            float stateMultiplier = isTravelling ? travelMultiplier : idleSpeedMultiplier;

            // Scale by the current tier so background visibly speeds up when the
            // player upgrades trains — not only at the Hyperloop branch above.
            float tierMultiplier = 1f;
            if (GameManager.Instance != null)
            {
                tierMultiplier = Mathf.Max(0.1f, GameManager.Instance.CurrentTierScrollMultiplier);
            }

            return Mathf.Max(0f, AmountOfParallax) * stateMultiplier * tierMultiplier;
        }
    }
}
