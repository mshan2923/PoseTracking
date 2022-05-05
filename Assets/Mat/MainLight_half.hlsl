/**
* worldPos : 월드 픽셀 포지션

* lightDir : 메인 라이트 방향 벡터
* lightColor0 : 메인 라이트 컬러
* distanceAtten : 컬링마스크에 의해 컬링되면 1, 아니면 0 (No확실)
* shadowAtten : Cast Shadow 값
**/

/*
void CustomLight_half(float3 worldPos, out half3 lightDir, out half3 lightColor0, out half distanceAtten, out half shadowAtten)
{
	//UNITY_INITIALIZE_OUTPUT(half3, lightColor0);

#if SHADERGRAPH_PREVIEW			//! 쉐이더 그래프 프리뷰에서 보이는 결과
	lightDir = half3(0.5, 0.5, 0);
	lightColor0 = 1;
	distanceAtten = 1;
	shadowAtten = 1;
#else

#if SHADOWS_SCREEN	//! Screen Space Shadow 사용할때 (No확실)
   half4 clipPos = TransformWorldToHClip(worldPos);
   half4 shadowCoord = ComputeScreenPos(clipPos);
#else
   half4 shadowCoord = TransformWorldToShadowCoord(worldPos);	//! 월드 공간좌표를 그림자 공간 좌표로 변환하는 함수 (No확실)
#endif

   Light mainLight = GetMainLight();			//! Lighting.hlsl 함수로 라이트 정보 및 쉐도우 데이터 구조체 얻어옴
   lightDir = normalize(mainLight.direction);	//! 메인 라이트 벡터
   lightColor0 = mainLight.color;				//! 메인 라이트 칼라

   distanceAtten = mainLight.distanceAttenuation;	//! 컬링마스크에 의해 컬링되면 1, 아니면 0 (No확실), 라이트맵 상황때는 다름
   //shadowAtten = mainLight.shadowAttenuation;		//! <ㅗ
   ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();	//! 쉐도우 감쇠값
   half4 shadowParams = GetMainLightShadowParams();
   shadowAtten = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), TransformWorldToShadowCoord(worldPos), shadowSamplingData, shadowParams, false);
#endif
}
*/

void CustomLight_half(float3 worldPos, out half3 LightDir, out half3 LightColor, out half DistanceAtten, out half ShadowAtten)
{
	half3 lightDir;
	UNITY_INITIALIZE_OUTPUT(half3, lightDir);
	half3 lightColor;
	UNITY_INITIALIZE_OUTPUT(half3, lightColor);
	UNITY_INITIALIZE_OUTPUT(half3, LightColor);
	half distanceAtten;
	UNITY_INITIALIZE_OUTPUT(half, distanceAtten);
	half shadowAtten;
	UNITY_INITIALIZE_OUTPUT(half, shadowAtten);

#if SHADERGRAPH_PREVIEW			//! 쉐이더 그래프 프리뷰에서 보이는 결과
	lightDir = half3(0.5, 0.5, 0);
	lightColor = 1;
	distanceAtten = 1;
	shadowAtten = 1;
#else

#if SHADOWS_SCREEN	//! Screen Space Shadow 사용할때 (No확실)
	half4 clipPos = TransformWorldToHClip(worldPos);
	half4 shadowCoord = ComputeScreenPos(clipPos);
#else
	half4 shadowCoord = TransformWorldToShadowCoord(worldPos);	//! 월드 공간좌표를 그림자 공간 좌표로 변환하는 함수 (No확실)
#endif

	Light mainLight = GetMainLight();			//! Lighting.hlsl 함수로 라이트 정보 및 쉐도우 데이터 구조체 얻어옴
	lightDir = normalize(mainLight.direction);	//! 메인 라이트 벡터
	lightColor = mainLight.color;				//! 메인 라이트 칼라

	distanceAtten = mainLight.distanceAttenuation;	//! 컬링마스크에 의해 컬링되면 1, 아니면 0 (No확실), 라이트맵 상황때는 다름
	//shadowAtten = mainLight.shadowAttenuation;		//! <ㅗ
	ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();	//! 쉐도우 감쇠값
	half4 shadowParams = GetMainLightShadowParams();
	shadowAtten = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), TransformWorldToShadowCoord(worldPos), shadowSamplingData, shadowParams, false);
#endif

	LightDir = lightDir;
	LightColor = lightColor;
	DistanceAtten = distanceAtten;
	ShadowAtten = shadowAtten;
}
