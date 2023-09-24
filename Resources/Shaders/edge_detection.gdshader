shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_disabled, diffuse_lambert, specular_disabled;

const int PULSE_MAX_COUNT = 32;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D depth_texture : hint_depth_texture, filter_linear_mipmap; 

uniform float normal_edge_detection_threshold : hint_range(-0.1, 0.1) = 0.001;
uniform float depth_edge_detection_threshold : hint_range(0.0, 10.0) = 2.0;
uniform float neighbour_distance : hint_range(0.0, 10.0) = 0.5;
uniform vec4 albedo : source_color;
uniform vec4 edge_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

float manhattan_distance(vec3 color_a, vec3 color_b) {
	vec3 difference = abs(color_a - color_b);
	return difference.r + difference.g + difference.b;
}

float linear_depth(float depth, vec2 screen_uv, mat4 inv_projection_matrix) {
	vec3 ndc = vec3(screen_uv * 2.0 - 1.0, depth);
	vec4 view = inv_projection_matrix * vec4(ndc, 1.0);
	view.xyz /= view.w;
	return -view.z;
}

vec2 pixel_to_screen_uv(vec2 viewport_size, vec2 pixel) {
	return vec2(pixel.x / viewport_size.x, pixel.y / viewport_size.y);
}

void fragment() {
	vec2 iuv = vec2(SCREEN_UV.x * VIEWPORT_SIZE.x, SCREEN_UV.y * VIEWPORT_SIZE.y);
	
	float depth_center = texture(depth_texture, SCREEN_UV).r;
	float depth_center_linear = linear_depth(depth_center, SCREEN_UV, INV_PROJECTION_MATRIX);
	if (depth_center_linear > 10.0) {
		ALBEDO = vec3(0, 0, 0);
	}
	else {
		vec2 uv_left = pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(-neighbour_distance, 0.0));
		vec2 uv_right = pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(neighbour_distance, 0.0));
		vec2 uv_bottom = pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(0.0, -neighbour_distance));
		vec2 uv_top = pixel_to_screen_uv(VIEWPORT_SIZE, iuv + vec2(0.0, neighbour_distance));
		
		vec3 normal_left = texture(screen_texture, uv_left).rgb;
		vec3 normal_right = texture(screen_texture, uv_right).rgb;
		vec3 normal_bottom = texture(screen_texture, uv_bottom).rgb;
		vec3 normal_top = texture(screen_texture, uv_top).rgb;
		
		float depth_left = texture(depth_texture, uv_left).r;
		float depth_right = texture(depth_texture, uv_right).r;
		float depth_bottom = texture(depth_texture, uv_bottom).r;
		float depth_top = texture(depth_texture, uv_top).r;
		
		float linear_depth_left = linear_depth(depth_left, SCREEN_UV, INV_PROJECTION_MATRIX);
		float linear_depth_right = linear_depth(depth_right, SCREEN_UV, INV_PROJECTION_MATRIX);
		float linear_depth_bottom = linear_depth(depth_bottom, SCREEN_UV, INV_PROJECTION_MATRIX);
		float linear_depth_top = linear_depth(depth_top, SCREEN_UV, INV_PROJECTION_MATRIX);
		
		if (manhattan_distance(normal_left, normal_right) > normal_edge_detection_threshold 
		 || manhattan_distance(normal_top, normal_bottom) > normal_edge_detection_threshold
		 || abs(linear_depth_left - linear_depth_right) > depth_edge_detection_threshold
		 || abs(linear_depth_bottom - linear_depth_top) > depth_edge_detection_threshold) {
			ALBEDO = edge_color.rgb;
		}
		else {
			ALBEDO = albedo.rgb;
		}
	}
}
