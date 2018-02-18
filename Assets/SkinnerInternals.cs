using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinner
{
    internal static class SkinnerInternals
    {
        public static RenderTextureFormat supportBufferFormat
        {
            get
            {
                #if UNITY_IOS || UNITY_TVOS || UNITY_ANDROID
                return RenderTextureFormat.ARGBHalf;
                #else
                return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ?
                        RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf;
                #endif
            }
        }
    }

    internal class AnimationKernelSet<KernelEnum, BufferEnum>
        where KernelEnum : struct
        where BufferEnum : struct
    {
        // ---------------
        #region Enum to int converter delegates
        public delegate int KernelEnumToInt(KernelEnum e);
        public delegate int BufferEnumToInt(BufferEnum e);
        #endregion

        // ---------------
        #region Private variables
        KernelEnumToInt _getKernelIndex;
        BufferEnumToInt _getBufferIndex;

        Shader _shader;
        Material _material;

        RenderTexture[] _buffers;
        bool _swapFlag;

        bool _ready;
        #endregion

        // ---------------
        #region Accessor properties and functions
        public Material material
        {
            get { return _material; }
        }

        public bool ready
        {
            get { return _ready; }
        }

        public RenderTexture GetLastBuffer(BufferEnum buffer)
        {
            var index = _getBufferIndex(buffer);
            return _buffers[_swapFlag ? index + _buffers.Length / 2 : index];
        }

        public RenderTexture GetWorkingBuffer(BufferEnum buffer)
        {
            var index = _getBufferIndex(buffer);
            return _buffers[_swapFlag ? index : index + _buffers.Length / 2];
        }
        #endregion

        // ---------------
        #region Public methods
        // Constructor
        public AnimationKernelSet(Shader shader, KernelEnumToInt k2i, BufferEnumToInt b2i)
        {
            _shader = shader;
            _getKernelIndex = k2i;
            _getBufferIndex = b2i;

            var enumCount = Enum.GetValues(typeof(BufferEnum)).Length;
            _buffers = new RenderTexture[enumCount * 2];
        }

        // Initialize the kernels and buffers
        public void Setup(int width, int height)
        {
            if (_ready) return;

            _material = new Material(_shader);

            var format = SkinnerInternals.supportBufferFormat;

            for (var i = 0; i < _buffers.Length; i++)
            {
                var rt = new RenderTexture(width, height, 0, format);
                rt.filterMode = FilterMode.Point;
                rt.wrapMode = TextureWrapMode.Clamp;
                _buffers[i] = rt;
            }

            _swapFlag = false;
            _ready = true;
        }

        public void Release()
        {
            if (!_ready) return;

            // Release Material
            UnityEngine.Object.Destroy(_material);
            _material = null;

            // Release RenderTexture Buffers
            for (var i = 0; i < _buffers.Length; i++)
            {
                UnityEngine.Object.Destroy(_buffers[i]);
                _buffers[i] = null;
            }

            _ready = false;
        }

        public void Invoke(KernelEnum kernel, BufferEnum buffer)
        {
            Graphics.Blit(null, GetWorkingBuffer(buffer), _material, _getKernelIndex(kernel));
        }

        public void SwapBuffers()
        {
            _swapFlag = !_swapFlag;
        }
        #endregion
    }


    internal class RendererAdapter
    {
        #region
        private GameObject _gameObject;
        private Material _defaultMaterial;
        private MaterialPropertyBlock _propertyBlock;
        #endregion

        #region
        public MaterialPropertyBlock propertyBlock
        {
            get { return _propertyBlock; }
        }
        #endregion

        #region Public methods
        // Constructor; initialize internal variables.
        public RendererAdapter(GameObject gameObject, Material defaultMaterial)
        {
            _gameObject = gameObject;
            _defaultMaterial = defaultMaterial;
            _propertyBlock = new MaterialPropertyBlock();
        }

        // Update MeshFilter and MeshRenderer.
        public void Update(Mesh templateMesh)
        {
            var meshFilter = _gameObject.GetComponent<MeshFilter>();

            // Add a new mesh filter if missing.
            if (meshFilter == null)
            {
                meshFilter = _gameObject.AddComponent<MeshFilter>();
                meshFilter.hideFlags = HideFlags.NotEditable;
            }

            // Set the template mesh if not set yet.
            if (meshFilter.sharedMesh != templateMesh)
                meshFilter.sharedMesh = templateMesh;

            var meshRenderer = _gameObject.GetComponent<MeshRenderer>();

            // Set the material if no material is set.
            if (meshRenderer.sharedMaterial == null)
                meshRenderer.sharedMaterial = _defaultMaterial;

            meshRenderer.SetPropertyBlock(_propertyBlock);
        }
        #endregion
    }
}