using UnityEngine;
using UnityEngine.UI;

namespace Buscaminas.Gameplay
{
    /// <summary>
    /// Manages the face icon in the HUD. Swaps between the happy and doh face
    /// sprites based on game win/lose/restart events from <see cref="GridManager"/>.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Image faceImage;
        [SerializeField] private Sprite happyFace;
        [SerializeField] private Sprite dohFace;

        void OnEnable()
        {
            if (gridManager != null)
            {
                gridManager.OnLose += HandleLose;
                gridManager.OnWin += HandleWin;
                gridManager.OnRestart += HandleRestart;
            }
        }

        void OnDisable()
        {
            if (gridManager != null)
            {
                gridManager.OnLose -= HandleLose;
                gridManager.OnWin -= HandleWin;
                gridManager.OnRestart -= HandleRestart;
            }
        }

        void HandleLose()
        {
            if (faceImage != null && dohFace != null)
            {
                faceImage.sprite = dohFace;
            }
        }

        void HandleWin()
        {
            if (faceImage != null && happyFace != null)
            {
                faceImage.sprite = happyFace;
            }
        }

        void HandleRestart()
        {
            if (faceImage != null && happyFace != null)
            {
                faceImage.sprite = happyFace;
            }
        }
    }
}
