using System.Collections.Generic;
using UnityEngine;

//The molecules the atom hooks its rope to. They float at any height, and new ones keep coming on the right.
//No prefabs: they are built from the sprites in the list
public class MoleculeAnchorsScript : MonoBehaviour
{
    public static MoleculeAnchorsScript Instance;

    public Sprite[] moleculeSprites;        //Empty = a white circle
    public float spriteScale = 0.55f;       //The same for every sprite, so all their pixels have the same size

    [Header("Placing")]
    public float minHeight = -3.2f;
    public float maxHeight = 3.6f;
    public float minGap = 1.8f;             //Units sideways between two molecules
    public float maxGap = 3.2f;
    public float startX = -4f;              //Where the first molecule is

    [Header("Floating")]
    public float bobHeight = 0.12f;
    public float bobSpeed = 0.6f;
    public float wobble = 8f;               //Degrees they rock

    class Anchor
    {
        public Transform transform;
        public Vector2 home;
        public float phase;
    }

    readonly List<Anchor> anchors = new List<Anchor>();
    float nextX;

    void Awake()
    {
        Instance = this;
        nextX = startX;
        Fill();     //Already there when the atom looks for its first molecule
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        Fill();

        Camera cam = Camera.main;
        float left = cam.transform.position.x - cam.orthographicSize * cam.aspect - 6f;

        for (int i = anchors.Count - 1; i >= 0; i--)
        {
            Anchor anchor = anchors[i];

            if (anchor.home.x < left)
            {
                Destroy(anchor.transform.gameObject);
                anchors.RemoveAt(i);
                continue;
            }

            float t = Time.time * bobSpeed * 2f * Mathf.PI + anchor.phase;
            anchor.transform.position = anchor.home + new Vector2(0f, Mathf.Sin(t) * bobHeight);
            anchor.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * wobble);
        }
    }

    //Molecules up to a bit past the right edge of the screen
    void Fill()
    {
        Camera cam = Camera.main;
        float right = cam.transform.position.x + cam.orthographicSize * cam.aspect + 6f;

        while (nextX < right)
        {
            Sprite sprite = moleculeSprites != null && moleculeSprites.Length > 0 ? moleculeSprites[Random.Range(0, moleculeSprites.Length)] : EffectsArt.Circle;

            SpriteRenderer renderer = EffectsArt.AddSprite("Molecule", transform, sprite, Color.white, 2, false);
            renderer.transform.localScale = Vector3.one * (sprite == EffectsArt.Circle ? 0.6f : spriteScale);
            renderer.flipX = Random.value < 0.5f;

            Vector2 home = new Vector2(nextX, Random.Range(minHeight, maxHeight));
            renderer.transform.position = home;

            anchors.Add(new Anchor { transform = renderer.transform, home = home, phase = Random.value * 10f });
            nextX += Random.Range(minGap, maxGap);
        }
    }

    //The molecule closest to a point (where the mouse clicked), no farther than pick from it and than reach from the atom
    public Transform Nearest(Vector2 point, Vector2 from, float reach, float pick)
    {
        Transform best = null;
        float bestDistance = pick;

        foreach (Anchor anchor in anchors)
        {
            Vector2 position = anchor.transform.position;
            float distance = Vector2.Distance(position, point);

            if (distance < bestDistance && Vector2.Distance(position, from) <= reach)
            {
                bestDistance = distance;
                best = anchor.transform;
            }
        }

        return best;
    }
}
