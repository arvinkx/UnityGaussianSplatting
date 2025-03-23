#ifndef GAUSSIAN_SPLAT_INCLUDES
#define GAUSSIAN_SPLAT_INCLUDES

#include "Assets/Shaders/Includes/MyUnityInstancing.cginc" // Your modified version

// Include UnityCG but be careful about what it might include
// We'll include the parts we need, and not the whole file
#include "UnityShaderVariables.cginc" // For common shader variables
#include "UnityInstancing.cginc" // Include again to make sure that the macros are defined.

#include "GaussianSplatting.hlsl" // Your other splatting includes

#endif