using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Stores, resets and restores the localPosition, localRotation and localScale values of a Transform 
     * 
     * @author J.C.Wichman - InnerDriveStudios.com
     */
    public struct TransformMemento
    {
        private Vector3 localPosition;
        private Quaternion localRotation;
        private Vector3 localScale;
        private Transform parent;
        private Transform transform;

        public TransformMemento(Transform pTransform)
        {
            transform = pTransform;

            parent = transform.parent;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        public void Reset()
        {
            transform.parent = null;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void Restore()
        {
            transform.parent = parent;
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }
    }
}