using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ThunderWire.Utility
{
    public class BlurController : MonoBehaviour
    {
        public string blurProperty = "_BlurSize";
        public float blurSpeed = 3f;

        private float blurSize = 0;
        private bool canBlur = false;

        private Material temp;

        void Start()
        {
            temp = new Material(GetComponent<Image>().material);

            if (temp)
            {
                if (temp.HasProperty(blurProperty))
                {
                    blurSize = temp.GetFloat(blurProperty);
                    canBlur = true;
                }
                else
                {
                    Debug.LogError($"[BlurController] Material shader does not have \"{blurProperty}\" property!");
                }
            }

            GetComponent<Image>().material = temp;
        }

        public void BlurMaterialIn(float size)
        {
            if (canBlur)
            {
                StopAllCoroutines();
                StartCoroutine(BlurMaterial(size, true));
            }
        }

        public void BlurMaterialOut(float size)
        {
            if (canBlur)
            {
                StopAllCoroutines();
                StartCoroutine(BlurMaterial(size, false));
            }
        }

        IEnumerator BlurMaterial(float size, bool inOut)
        {
            while (inOut ? blurSize < size : blurSize > size)
            {
                blurSize = temp.GetFloat(blurProperty);
                temp.SetFloat(blurProperty, Mathf.MoveTowards(blurSize, size, Time.deltaTime * blurSpeed));
                yield return null;
            }

            temp.SetFloat(blurProperty, size);
        }
    }
}
