using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Runtime-spawned effects (bullets, shield bubble) need *some* visible sprite -
    // a SpriteRenderer with no sprite assigned draws nothing. Builds one antialiased
    // circle once instead of requiring an art asset for a sandbox prototype.
    public static class ProceduralSprites
    {
        private const int Size = 64;

        private static Sprite _circle;
        public static Sprite Circle => _circle ??= BuildCircle();

        private static Sprite BuildCircle()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = (Size - 1) * 0.5f;
            var radius = Size * 0.5f;
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = Mathf.Clamp01(radius - d);
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            // pixelsPerUnit = Size -> the sprite is exactly 1 world unit wide, matching
            // every spawner's existing `localScale = Vector3.one * (radius * 2f)`.
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
