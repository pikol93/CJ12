#define EDITOR

shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_disabled, diffuse_lambert, specular_disabled;

const int PULSE_MAX_COUNT = 256;
const int VISIBLE = 0;
const int INVISIBLE = 1;
const int EDGE_CHECK = 2;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D depth_texture : hint_depth_texture, filter_linear_mipmap; 

uniform float pulse_width : hint_range(0.0, 0.1) = 0.05;
uniform float normal_edge_detection_threshold : hint_range(-0.1, 0.1) = 0.001;
uniform float depth_edge_detection_threshold : hint_range(0.0, 10.0) = 2.0;
uniform float neighbour_distance : hint_range(0.0, 10.0) = 0.5;
uniform vec4 albedo : source_color;
uniform vec4 edge_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform bool always_visible = false;

// Variables set by script
uniform bool force_edge_check = false;
uniform vec3[PULSE_MAX_COUNT] pulse_positions;
uniform float[PULSE_MAX_COUNT] pulse_timestamps;
uniform float[PULSE_MAX_COUNT] pulse_velocities;
uniform float[PULSE_MAX_COUNT] pulse_max_ranges;
uniform float[PULSE_MAX_COUNT] pulse_max_lifetimes;

varying mat4 CAMERA;

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

void vertex() {
	CAMERA = INV_VIEW_MATRIX;
}

void fragment() {
	vec2 iuv = vec2(SCREEN_UV.x * VIEWPORT_SIZE.x, SCREEN_UV.y * VIEWPORT_SIZE.y);
	
	float depth = texture(depth_texture, SCREEN_UV).x;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth_var = -view.z;
	vec4 world = CAMERA * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	vec3 pixel_world_position = world.xyz / world.w;
	
#ifdef EDITOR
	int visibility = VISIBLE;
#else
	int visibility = INVISIBLE;
#endif
	for (int i = 0; i < PULSE_MAX_COUNT; i++) {
		if (visibility == VISIBLE) {
			break;
		}
		
		float timestamp = pulse_timestamps[i];
		if (timestamp < 0.01) {
			break;
		}
		float pulse_lifetime = TIME - timestamp;
		float velocity = pulse_velocities[i];
		float max_range = pulse_max_ranges[i];
		float range = min(max_range, velocity * pulse_lifetime);
		vec3 pulsePosition = pulse_positions[i];
		float distanceToPulse = distance(pulsePosition, pixel_world_position);
		if (distanceToPulse < range) {
			if (distanceToPulse < range - pulse_width) {
				visibility = EDGE_CHECK;
			}
			else {
				visibility = VISIBLE;
			}
		}
	}
	
	if (force_edge_check && visibility == INVISIBLE) {
		visibility = EDGE_CHECK;
	}
	
	if (visibility == INVISIBLE) {
		ALBEDO = vec3(0, 0, 0);
	}
	else if (visibility == VISIBLE) {
		ALBEDO = edge_color.rgb;	
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
