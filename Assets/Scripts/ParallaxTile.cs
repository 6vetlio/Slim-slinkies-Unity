using UnityEngine;

namespace _Scripts
{
    public class ParallaxTile : MonoBehaviour
    {
        public float Speed = 5f;
        public float TileWidth;

        private void Start()
        {
            TileWidth = GetComponent<SpriteRenderer>().bounds.size.x;
        }

        private void Update()
        {
            // Move world left
            transform.position += Vector3.left * Speed * Time.deltaTime;

            // Wrap when offscreen
            if (transform.position.x <= -TileWidth)
            {
                transform.position += Vector3.right * TileWidth * 2f;
            }
        }
    }
}