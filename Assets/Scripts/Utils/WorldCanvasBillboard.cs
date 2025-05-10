using System;
using UnityEngine;

namespace Utils
{
    public class WorldCanvasBillboard : MonoBehaviour
    {
        private Camera mainCam;
        
        private void Awake()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            transform.forward = mainCam.transform.forward;
        }
    }
}
