shader_type spatial;

uniform float MinHeight;
uniform float MaxHeight;
uniform sampler2D HeightColor;

varying float height;

void vertex() {
    height = length(VERTEX);
}

void fragment() {
    float range = MaxHeight - MinHeight;
    float t = height / range - MinHeight / range;
    vec3 color = texture(HeightColor, vec2(t, UV.y)).rgb;
    ALBEDO = vec3(color);
}
