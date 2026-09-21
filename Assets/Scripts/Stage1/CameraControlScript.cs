using UnityEngine;

public class CameraControlScript : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public SpriteRenderer sprite;   //Must be a child of the camera, with Draw Mode set to Tiled

        [Range(0f, 1f)]
        public float parallax = 0.5f;   //0 = fixed on screen (far away), 1 = moves like the planets (close)

        [HideInInspector] public Vector2 tileSize;
        [HideInInspector] public float localZ;
    }

    public Transform target;
    public float smoothTime = 0.25f;    //Higher = the camera lags more behind the player

    public ParallaxLayer[] layers;

    Vector3 velocity;

    void Start()
    {
        transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);   //Start on the player, no initial sweep

        foreach (ParallaxLayer layer in layers)
        {
            Sprite sprite = layer.sprite.sprite;
            layer.tileSize = Vector2.Scale(sprite.rect.size / sprite.pixelsPerUnit, layer.sprite.transform.lossyScale);   //Rect and not bounds: with a Tight mesh the bounds only wrap the stars
            layer.localZ = layer.sprite.transform.localPosition.z;
        }
    }

    void LateUpdate()
    {
        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        foreach (ParallaxLayer layer in layers)
        {
            Quaternion rotation = layer.sprite.transform.localRotation;
            Vector3 offset = new Vector3(-transform.position.x, -transform.position.y, 0f) * layer.parallax;   //Sliding back by a fraction of the camera movement
            offset = Quaternion.Inverse(rotation) * offset;   //A rotated layer has a rotated tile grid, so we wrap along its own axes

            offset.x = Mathf.Repeat(offset.x + layer.tileSize.x / 2f, layer.tileSize.x) - layer.tileSize.x / 2f;   //Wrap by one tile so the layer never runs out
            offset.y = Mathf.Repeat(offset.y + layer.tileSize.y / 2f, layer.tileSize.y) - layer.tileSize.y / 2f;

            offset = rotation * offset;
            layer.sprite.transform.localPosition = new Vector3(offset.x, offset.y, layer.localZ);
        }
    }
}
