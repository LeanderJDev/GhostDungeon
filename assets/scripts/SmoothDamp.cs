using System;
using Godot;

namespace Musikspieler.Scripts.RecordView
{
    /// <summary>
    /// Klasse, die drei Parameter speichert, um SmoothDamp auf allen drei Transform3D-Komponenten auszuführen.
    /// </summary>
    /// Als Klasse (nicht struct), damit mehrere Objekte die gleichen Parameter nutzen können, und Änderungen durch die Referenz sofort Effekt haben.
    public class SmoothDamp
    {
        /// <summary>
        /// Eine Glättungsfunktion, um eine Zielposition über Zeit mit glatter Bewegung zu erreichen.
        /// </summary>
        /// <param name="current">Die aktuelle PositionParameters</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue PositionParameters.</returns>
        public static Vector2 Step(
            Vector2 current,
            Vector2 target,
            ref Vector2 currentVelocity,
            float smoothTime,
            float maxSpeed,
            float deltaTime
        )
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float maxChange = maxSpeed * smoothTime;
            Vector2 change = target - current;
            float changeLength = Mathf.Max(change.Length(), 0.000001f);
            change *= Mathf.Min(changeLength, maxChange) / changeLength;
            Vector2 temp = (currentVelocity - omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            Vector2 output = current + change + (temp - change) * exp;
            if (change.Dot(target - output) < 0)
            {
                output = target;
                currentVelocity = (output - target) / deltaTime;
            }
            return output;
        }
    }
}
