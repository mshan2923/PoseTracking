half3 LightingSubsurface( half3 normalWS, half3 subsurfaceColor, half subsurfaceRadius) {
    // Calculate normalized wrapped lighting. This spreads the light without adding energy.
    // This is a normal lambertian lighting calculation (using N dot L), but warping NdotL
    // to wrap the light further around an object.
    //
    // A normalization term is applied to make sure we do not add energy.
    // http://www.cim.mcgill.ca/~derek/files/jgt_wrap.pdf
    // https://johnaustin.io/articles/2020/fast-subsurface-scattering-for-the-unity-urp
    Light light = GetMainLight();

    half NdotL = dot(normalWS, light.direction);
    half alpha = subsurfaceRadius;
    half theta_m = acos(-alpha); // boundary of the lighting function

    half theta = max(0, NdotL + alpha) - alpha;
    half normalization_jgt = (2 + alpha) / (2 * (1 + alpha));
    half wrapped_jgt = (pow(((theta + alpha) / (1 + alpha)), 1 + alpha)) * normalization_jgt;

    half wrapped_valve = 0.25 * (NdotL + 1) * (NdotL + 1);
    half wrapped_simple = (NdotL + alpha) / (1 + alpha);

    half3 subsurface_radiance = light.color * subsurfaceColor * wrapped_jgt;

    return subsurface_radiance;
}