#if defined(VERTEX) || __VERSION__ > 100 || defined(GL_FRAGMENT_PRECISION_HIGH)
	#define PRECISION highp
#else
	#define PRECISION mediump
#endif

extern PRECISION vec2 blackhole;

extern PRECISION number dissolve;
extern PRECISION number time;
extern PRECISION vec4 texture_details;
extern PRECISION vec2 image_details;
extern bool shadow;
extern PRECISION vec4 burn_colour_1;
extern PRECISION vec4 burn_colour_2;

vec4 dissolve_mask(vec4 final_pixel, vec2 texture_coords, vec2 uv);


vec4 effect( vec4 colour, Image texture, vec2 texture_coords, vec2 screen_coords )
{
	vec2 uv = (((texture_coords)*(image_details)) - texture_details.xy*texture_details.ba)/texture_details.ba;
    float x = 20*(uv.x-0.5);
    float y = -(uv.y-0.5)*20*1.338;

    if (sqrt(x*x + y*y) <= 2){
        colour.r = 0;
        colour.g = 0;
        colour.b = 0;
    }

    if (sqrt(x*x + y*y) >= 2){
        colour.r = colour.r*(1 + 0.000004*(x*x + y*y)*(x*x + y*y) - 1.5/(sqrt(x*x + y*y) + 1));
        colour.g = colour.g*(1 - 1.5/(sqrt(x*x + y*y) + 1));
        colour.b = colour.b*(1 - 1.5/(sqrt(x*x + y*y) + 1));
    }

    float sprite_width = texture_details.z / image_details.x;
    float sprite_length = texture_details.z / (image_details.y/1.338);
    float min_x = texture_details.x * sprite_width;
    float min_y = texture_details.y * sprite_length;
    float max_x = (texture_details.x + 1.) * sprite_width;
    float max_y = (texture_details.y + 1.) * sprite_length;
    float scaled_uvy = (uv.y -0.5)*8;
    float scaled_uvx = (uv.x -0.5)*5;
    float black_holey = 1/(1 + scaled_uvy*scaled_uvy);
    float black_holex = 0.7/(1 + scaled_uvx*scaled_uvx);
    float floor_uvx = (uv.x - 0.5)*2;
    float floor_uvy = (uv.y - 0.5)*2;

    float tilt_normalized = blackhole.x*0.0000000000001;
    float tilt_normalizedy = blackhole.y*0.0000000000001;

    float shiftX = floor_uvx * black_holey * ((10.16 + 3.55 * sin(1.5 * tilt_normalized)) + tilt_normalized * 1.5)
                                            / image_details.x;
    float shiftY = floor_uvy * black_holex * ((10.16 + 3.55 * sin(1.5 * tilt_normalizedy)) + tilt_normalizedy * 1.5)
                                            / image_details.y;

    float newX = min(max_x, max(min_x, texture_coords.x + shiftX));
    float newY = min(max_y, max(min_y, texture_coords.y + shiftY));
    
    vec4 pixel = Texel(texture, vec2(newX, newY));
    
	return dissolve_mask(pixel*colour, texture_coords, uv);
}

vec4 dissolve_mask(vec4 final_pixel, vec2 texture_coords, vec2 uv)
{
    if (dissolve < 0.001) {
        return vec4(shadow ? vec3(0.,0.,0.) : final_pixel.xyz, shadow ? final_pixel.a*0.3: final_pixel.a);
    }

    float adjusted_dissolve = (dissolve*dissolve*(3.-2.*dissolve))*1.02 - 0.01;

	float t = time * 10.0 + 2003.;
	vec2 floored_uv = (floor((uv*texture_details.ba)))/max(texture_details.b, texture_details.a);
    vec2 uv_scaled_centered = (floored_uv - 0.5) * 2.3 * max(texture_details.b, texture_details.a);
	
	vec2 field_part1 = uv_scaled_centered + 50.*vec2(sin(-t / 143.6340), cos(-t / 99.4324));
	vec2 field_part2 = uv_scaled_centered + 50.*vec2(cos( t / 53.1532),  cos( t / 61.4532));
	vec2 field_part3 = uv_scaled_centered + 50.*vec2(sin(-t / 87.53218), sin(-t / 49.0000));

    float field = (1.+ (
        cos(length(field_part1) / 19.483) + sin(length(field_part2) / 33.155) * cos(field_part2.y / 15.73) +
        cos(length(field_part3) / 27.193) * sin(field_part3.x / 21.92) ))/2.;
    vec2 borders = vec2(0.2, 0.8);

    float res = (.5 + .5* cos( (adjusted_dissolve) / 82.612 + ( field + -.5 ) *3.14))
    - (floored_uv.x > borders.y ? (floored_uv.x - borders.y)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.y > borders.y ? (floored_uv.y - borders.y)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.x < borders.x ? (borders.x - floored_uv.x)*(5. + 5.*dissolve) : 0.)*(dissolve)
    - (floored_uv.y < borders.x ? (borders.x - floored_uv.y)*(5. + 5.*dissolve) : 0.)*(dissolve);

    if (final_pixel.a > 0.01 && burn_colour_1.a > 0.01 && !shadow && res < adjusted_dissolve + 0.8*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
        if (!shadow && res < adjusted_dissolve + 0.5*(0.5-abs(adjusted_dissolve-0.5)) && res > adjusted_dissolve) {
            final_pixel.rgba = burn_colour_1.rgba;
        } else if (burn_colour_2.a > 0.01) {
            final_pixel.rgba = burn_colour_2.rgba;
        }
    }

    return vec4(shadow ? vec3(0.,0.,0.) : final_pixel.xyz, res > adjusted_dissolve ? (shadow ? final_pixel.a*0.3: final_pixel.a) : .0);
}

extern PRECISION vec2 mouse_screen_pos;
extern PRECISION float hovering;
extern PRECISION float screen_scale;

#ifdef VERTEX
vec4 position( mat4 transform_projection, vec4 vertex_position )
{
    if (hovering <= 0.){
        return transform_projection * vertex_position;
    }
    float mid_dist = length(vertex_position.xy - 0.5*love_ScreenSize.xy)/length(love_ScreenSize.xy);
    vec2 mouse_offset = (vertex_position.xy - mouse_screen_pos.xy)/screen_scale;
    float scale = 0.2*(-0.03 - 0.3*max(0., 0.3-mid_dist))
                *hovering*(length(mouse_offset)*length(mouse_offset))/(2. -mid_dist);

    return transform_projection * vertex_position + vec4(0,0,0,scale);
}
#endif