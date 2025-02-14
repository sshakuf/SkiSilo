using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

public class LegController : MonoBehaviour
{
    public Transform leftLeg;
    public Transform rightLeg;

    private Quaternion initialLeftRotation;
    private Quaternion initialRightRotation;
    private Quaternion targetLeftRotation;
    private Quaternion targetRightRotation;
    private bool hasNewData = false;

    private void Start()
    {
        initialLeftRotation = leftLeg.rotation;
        initialRightRotation = rightLeg.rotation;
    }

    private void OnEnable()
    {
        UDPModel.OnDataReceived += HandleNewRotationData;
    }

    private void OnDisable()
    {
        UDPModel.OnDataReceived -= HandleNewRotationData;
    }

    private void HandleNewRotationData(Quaternion leftRotation, Quaternion rightRotation)
    {
        targetLeftRotation = initialLeftRotation * leftRotation;
        targetRightRotation = initialRightRotation * rightRotation;
        hasNewData = true;
    }

    private void LateUpdate()
    {
        if (hasNewData)
        {
            leftLeg.rotation = targetLeftRotation;
            rightLeg.rotation = targetRightRotation;
            hasNewData = false;
        }
    }
}