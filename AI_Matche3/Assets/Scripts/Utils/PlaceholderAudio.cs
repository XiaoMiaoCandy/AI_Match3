using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 占位音频：代码合成一段柔和的循环旋律（C-Am-F-G），作为开始界面默认 BGM。
    /// 在 GameConfig 的 backgroundMusic 中配置真实音频后即被替换。
    /// </summary>
    public static class PlaceholderAudio
    {
        public static AudioClip CreateLoop()
        {
            const int sampleRate = 44100;
            const float chordTime = 1.6f; // 每个和弦时长(秒)

            // C - Am - F - G 和弦（三音）
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392.00f },
                new[] { 220.00f, 261.63f, 329.63f },
                new[] { 174.61f, 220.00f, 261.63f },
                new[] { 196.00f, 246.94f, 293.66f },
            };

            int total = Mathf.CeilToInt(chordTime * chords.Length * sampleRate);
            var data = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)sampleRate;
                int ci = Mathf.Min((int)(t / chordTime), chords.Length - 1);
                float lt = t - ci * chordTime;

                // 淡入淡出包络，避免爆音，循环处自然衔接
                float env = Mathf.SmoothStep(0f, 1f, Mathf.Min(lt / 0.15f, 1f)) *
                            Mathf.SmoothStep(0f, 1f, Mathf.Min((chordTime - lt) / 0.15f, 1f));

                float s = 0f;
                foreach (var f in chords[ci]) s += Mathf.Sin(2f * Mathf.PI * f * t) * 0.22f;
                data[i] = s * env;
            }

            var clip = AudioClip.Create("PlaceholderBGM", total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
