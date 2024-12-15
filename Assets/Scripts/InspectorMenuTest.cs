using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class InspectorMenuTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    

    public enum Colors
    {
        White,
        Red,
        Green,
        Blue,
    }

    public enum Type
    {
        Square,
        Circle,
        Triangle,
    }

    [Header("General Settings")]
    public string Name;

    [TextArea]
    public string Description;

    public Type TypeOfThing;

    [Header("Color Settings")]
    [Tooltip("What color to make this thing")]
    [EnumButtons]
    public Colors Color;

    [EnumButtons]
    public List<Colors> AllowedColors;

    [Header("Movement Settings")]
    [Min(0)]
    [Tooltip("Cannot be less that 0")]
    public float GravitySettings;

    [UnityEngine.Range(0f, 10f)]
    public float Speed;

    




}
