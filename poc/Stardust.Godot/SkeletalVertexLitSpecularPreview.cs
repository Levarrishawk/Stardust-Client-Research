using Stardust.Core.Assets;

internal static class SkeletalVertexLitSpecularPreview
{
    internal static string Code(DdsColorSpace space,bool scalesBloom)=>SkeletalAlbedoPreview.Header+
        "uniform sampler2D specular_texture : filter_linear_mipmap, repeat_enable; "+
        "uniform vec3 light_direction_world=vec3(0.0,0.0,1.0); uniform vec3 ambient_color=vec3(0.35); uniform vec3 directional_color=vec3(0.75); "+
        "uniform vec3 directional_specular=vec3(0.75); uniform vec3 vertex_specular=vec3(0.0); uniform vec3 material_specular=vec3(1.0); uniform float material_power=300.0; uniform vec3 emissive_color=vec3(0.0); "+
        "uniform bool bloom_enabled=false; uniform bool fade_enabled=false; uniform float fade_opacity=1.0; uniform bool visualize_alpha=false; "+
        "uniform bool fixed_vertex_lighting=false; uniform bool fixed_half_angle=false; uniform vec3 half_angle_world=vec3(0.0,0.0,1.0); varying vec3 vertex_diffuse_preview; varying vec3 vertex_specular_preview; "+
        CellLightingGpuPreview.Uniforms+CellLightingGpuPreview.Functions+
        "void vertex() { vec3 world_normal=normalize(MODEL_NORMAL_MATRIX*NORMAL); vec3 world_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; vec3 viewer=normalize(INV_VIEW_MATRIX[3].xyz-world_position); "+
        "vec3 half_sum=fixed_half_angle?half_angle_world:light_direction_world+viewer; vec3 half_direction=half_sum/max(length(half_sum),0.000001); float n_l=dot(world_normal,light_direction_world); "+
        "vertex_diffuse_preview=ambient_color+emissive_color+max(n_l,0.0)*directional_color; float intensity=n_l>0.0?pow(clamp(dot(world_normal,half_direction),0.0,1.0),material_power):0.0; "+
        "vertex_specular_preview=intensity*directional_specular*material_specular; if(cell_lighting_enabled){cell_lighting(world_position,world_normal,INV_VIEW_MATRIX[3].xyz,vertex_diffuse_preview,vertex_specular_preview);vertex_diffuse_preview+=emissive_color;} if(fixed_vertex_lighting){vertex_diffuse_preview=ambient_color;vertex_specular_preview=vertex_specular;} } "+
        "void fragment() { vec4 sample_value=texture(albedo_texture,UV); vec3 hue=alpha_hue_mask?mix(first_factor,second_factor,sample_value.a):first_factor; vec3 specular_raw=clamp(vertex_specular_preview,vec3(0.0),vec3(1.0))*texture(specular_texture,UV).a; "+
        "vec3 rgb=sample_value.rgb*hue*clamp(vertex_diffuse_preview,vec3(0.0),vec3(1.0))+specular_raw"+(scalesBloom?"*(bloom_enabled?0.5:1.0)":"")+"; "+
        "float result_alpha=fade_enabled?fade_opacity:dot(specular_raw,vec3(0.3,0.59,0.11))*(bloom_enabled?0.65:1.0); if(visualize_alpha)rgb=vec3(result_alpha); ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(space)+"; }";
}
