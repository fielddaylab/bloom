using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Zavala.UI
{
    public class UIPieChart : MonoBehaviour
    {
        /*
        static private int CFERTILIZER_INDEX = 0;
        static private int MANURE_INDEX = 1;
        static private int DFERTILIZER_INDEX = 2;
        */

        [SerializeField] private Transform m_Root;
        [SerializeField] private Image m_BG;
        [SerializeField] private Image[] m_Portions;
        [SerializeField] private Image[] m_PortionIcons;

        [SerializeField] private float m_IconInset = 20;

        private float[] m_Ratios;

        private void Start() {
            SetAmounts(new int[3] { 5, 5, 3 });
        }

        public void SetAmounts(int[] amounts) {
            RecalculateRatios(amounts);
            RefreshVisuals();
        }

        private void RecalculateRatios(int[] amounts) {
            int total = 0;

            for (int i = 0; i < amounts.Length; i++) {
                total += amounts[i];
            }

            if (total <= 0) {
                total = 1;
            }

            if (m_Ratios == null) {
                m_Ratios = new float[amounts.Length];
            }

            for (int i = 0; i < m_Ratios.Length; i++) {
                m_Ratios[i] = (float)amounts[i] / total;
            }
        }

        private void RefreshVisuals() {
            if (m_Ratios.Length > m_Portions.Length) {
                Debug.Log("[PieChart] Not enough portions for the amount of data passed in!");
                return;
            }

            Vector3 center = m_BG.transform.position;
            float radius = m_Portions[0].rectTransform.rect.width / 2 - m_IconInset;
            float zRotation = 0;
            float prevRotation = 0;
            for (int i = 0; i < m_Ratios.Length; i++) {
                m_Portions[i].fillAmount = m_Ratios[i];
                m_Portions[i].transform.rotation = Quaternion.Euler(new Vector3(0, 0, zRotation));
                prevRotation = zRotation;
                zRotation -= m_Ratios[i] * 360;

                // Position icons
                float rad = Mathf.Deg2Rad * ((zRotation + prevRotation) / 2 + 90);
                float x = Mathf.Cos(rad) * radius;
                float y = Mathf.Sin(rad) * radius;
                m_PortionIcons[i].transform.localPosition = new Vector3(x, y, 0);
                m_PortionIcons[i].gameObject.SetActive(m_Ratios[i] > 0);
            }
        }
    }
}
