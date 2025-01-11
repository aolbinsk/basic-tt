using UnityEngine;
using Domain.Interfaces;
using System;
using Domain.Entities;

namespace Infrastructure.XRInput
{
    /// <summary>
    /// An adapter that converts local XR input data to world space coordinates.
    /// Implements IInputManager and wraps a core input manager.
    /// </summary>
    public class WorldSpaceAdapterInputManager : IInputManager
    {
        private readonly IInputManager _coreInputManager;
        private readonly Transform _xrRigTransform;

        /// <summary>
        /// Initializes a new instance of the WorldSpaceAdapterInputManager class.
        /// </summary>
        /// <param name="coreInputManager">The core input manager providing local XR data.</param>
        /// <param name="xrRigTransform">The transform of the XR rig.</param>
        public WorldSpaceAdapterInputManager(IInputManager coreInputManager, Transform xrRigTransform)
        {
            _coreInputManager = coreInputManager;
            _xrRigTransform = xrRigTransform;
        }

        public void ReadLeftControllerState(ref ControllerState controllerState)
        {
            _coreInputManager.ReadLeftControllerState(ref controllerState);
            controllerState.Position = _xrRigTransform.TransformPoint(controllerState.Position);
            controllerState.Rotation = _xrRigTransform.rotation * controllerState.Rotation;
            // TODO: Does the velocity and angular velcity need to be transformed?
        }

        public void ReadRightControllerState(ref ControllerState controllerState)
        {
            _coreInputManager.ReadRightControllerState(ref controllerState);
            controllerState.Position = _xrRigTransform.TransformPoint(controllerState.Position);
            controllerState.Rotation = _xrRigTransform.rotation * controllerState.Rotation;
            // TODO: Does the velocity and angular velcity need to be transformed?
        }

        public bool LeftGripPressed => _coreInputManager.LeftGripPressed;

        public bool RightGripPressed => _coreInputManager.RightGripPressed;

        public void StoreLeftInputSample(Vector3 position, Quaternion rotation, float time)
        {
            Vector3 localPosition = _xrRigTransform.InverseTransformPoint(position);
            Quaternion localRotation = Quaternion.Inverse(_xrRigTransform.rotation) * rotation;
            _coreInputManager.StoreLeftInputSample(localPosition, localRotation, time);
        }

        public Vector3 CalculateLeftControllerVelocity()
        {
            Vector3 localVelocity = _coreInputManager.CalculateLeftControllerVelocity();
            return _xrRigTransform.TransformDirection(localVelocity);
        }

        public void ClearLeftInputSamples()
        {
            _coreInputManager.ClearLeftInputSamples();
        }

        public void Dispose()
        {
            _coreInputManager.Dispose();
        }
    }
}