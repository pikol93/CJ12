shader_type spatial;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
render_mode blend_mix, depth_prepass_alpha, cull_back, diffuse_lambert, specular_disabled;
uniform float edge_detection_threshold : hint_range(-0.1, 0.1) = 0.001;
uniform float neighbour_distance : hint_range(0.0, 10.0) = 0.5;
uniform vec4 albedo : source_color;
uniform vec4 edge_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

float manhattan_distance(vec3 color_a, vec3 color_b) {
	vec3 difference = abs(color_a - color_b);
	return difference.r + difference.y + difference.z;
}

vec2 pixel_to_screen_uv(vec2 viewport_size, vec2 pixel) {
	return vec2(pixel.x / viewport_size.x, pixel.y / viewport_size.y);
}

void fragment() {
	vec2 iuv = vec2(SCREEN_UV.x * VIEWPORT_SIZE.x, SCREEN_UV.y * VIEWPORT_SIZE.y);
	
	vec3 neighbour_left = texture(SCREEN_TEXTURE, pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(-neighbour_distance, 0))).rgb;
	vec3 neighbour_right = texture(SCREEN_TEXTURE, pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(neighbour_distance, 0))).rgb;
	
	vec3 neighbour_top = texture(SCREEN_TEXTURE, pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(0, neighbour_distance))).rgb;
	vec3 neighbour_bottom = texture(SCREEN_TEXTURE, pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(0, -neighbour_distance))).rgb;
	
	if (manhattan_distance(neighbour_left, neighbour_right) > edge_detection_threshold) {
		ALBEDO = edge_color.rgb;
	}
	else if (manhattan_distance(neighbour_top, neighbour_bottom) > edge_detection_threshold) {
		ALBEDO = edge_color.rgb;
	}
	else {
		ALBEDO = albedo.rgb;
	}
}
