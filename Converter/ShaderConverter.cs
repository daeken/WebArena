using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoreLinq;
using Newtonsoft.Json;
using static System.Console;

namespace Converter {
	public class JTexture {
		public bool Clamp { get; set; }
		public string Texture { get; set; }
		public string[] AnimTex { get; set; }
		public double Frequency { get; set; }
	}
	public class JLayer {
		public int[] Blend { get; set; }
		public JTexture[] Textures { get; set; }
		public string FragShader { get; set; }
		public bool? DepthWrite { get; set; }
		public bool AlphaTested { get; set; }
	}

	public class ShaderConverter {
		internal static string FloatString(float value) {
			var temp = value.ToString();
			return !temp.Contains(".") ? $"{temp}." : temp;
		}
		
		internal static string FloatString(double value) {
			var temp = value.ToString();
			return !temp.Contains(".") ? $"{temp}." : temp;
		}
		
		class Compiler {
			class Layer {
				internal LayerCommand Texture;
				internal Gen RgbGen = Gen.Identity;
				internal Gen AlphaGen = Gen.Identity;
				internal TcGen TcGen = TcGen.Base;
				internal readonly List<TcMod> TcMods = new List<TcMod>();
				
				internal AlphaFunc AlphaFunc;
				internal (BlendFactor S, BlendFactor D) BlendFunc = (BlendFactor.One, BlendFactor.Zero);

				internal DepthFunc DepthFunc;
				internal bool? DepthWrite;
			}

			class LayerStack {
				class CodeGen {
					readonly Dictionary<string, string>
						Uniforms = new Dictionary<string, string>(), Varyings = new Dictionary<string, string>();
					readonly List<string> Functions = new List<string>();
					readonly Dictionary<string, List<string>> Body = new Dictionary<string, List<string>>();

					internal void AddUniform(string type, string name) => Uniforms[name] = type;
					internal void AddVarying(string type, string name) => Varyings[name] = type;
					internal void AddFunction(string func) {
						if(!string.IsNullOrEmpty(func))
							Functions.Add(func);
					}
					internal void AddStmt(string func, string stmt) {
						if(!Body.ContainsKey(func)) Body[func] = new List<string>();
						Body[func].Add(stmt);
					}

					internal string Generate() {
						var ret = new List<string>();
					
						foreach(var (name, type) in Uniforms) ret.Add($"uniform {type} {name};");
						foreach(var (name, type) in Varyings) ret.Add($"varying {type} {name};");
						foreach(var func in Functions) ret.Add(func);
						foreach(var (name, stmts) in Body.OrderBy(x => x.Key == "main")) {
							ret.Add(name == "main" ? "void main() {" : $"vec4 {name}() {{");
							foreach(var stmt in stmts) ret.Add($"\t{stmt}");
							ret.Add("}");
						}

						return ret.Join("\n");
					}
				}

				readonly int StackI;
				internal readonly List<Layer> Layers;
				internal readonly List<LayerCommand> Textures = new List<LayerCommand>();
				internal readonly (BlendFactor, BlendFactor) BlendFunc = (BlendFactor.One, BlendFactor.Zero);
				internal readonly bool? DepthWrite;
				internal readonly DepthFunc DepthFunc;
				internal readonly bool AlphaTested;

				internal LayerStack(int stackI, params Layer[] layers) {
					StackI = stackI;
					Layers = layers.ToList();
					foreach(var layer in Layers) {
						switch(layer.Texture) {
							case LayerCommand.Map map:
								if(map.Item.IsTexture)
									Textures.Add(map);
								break;
							case LayerCommand.ClampMap map:
								Textures.Add(map);
								break;
							case LayerCommand.AnimMap map:
								Textures.Add(map);
								break;
						}
					}
					BlendFunc = Layers.First().BlendFunc;
					DepthWrite = Layers.First().DepthWrite;
					DepthFunc = Layers.First().DepthFunc;
					AlphaTested = Layers.First().AlphaFunc != null;
				}

				readonly Dictionary<string, string> Prebuilts = new Dictionary<string, string> {
					["inversesawtooth"] = @"
float inversesawtoothWave(float base, float amp, float phase, float freq) {
	return clamp(base + (1.0 - mod(uTime * freq + phase, 1.)) * amp, 0., 1.);
}", 
					["sawtooth"] = @"
float sawtoothWave(float base, float amp, float phase, float freq) {
	return clamp(base + mod(uTime * freq + phase, 1.) * amp, 0., 1.);
}", 
					["sin"] = @"
float sinWave(float base, float amp, float phase, float freq) {
	return clamp(base + sin(uTime * 3.14159 * 2. * freq + phase) * amp, 0., 1.);
}", 
					["square"] = @"
float squareWave(float base, float amp, float phase, float freq) {
	return clamp(base + sign(sin(uTime * 3.14159 * 2. * freq + phase)) * amp, 0., 1.);
}", 
					["triangle"] = @"
float triangleWave(float base, float amp, float phase, float freq) {
	return clamp(base + (1. - (abs(mod(uTime * freq + phase, 1.) - 0.5) * 2.)) * amp, 0., 1.);
}", 
					["rotate"] = @"
vec2 rotate(vec2 v, float deg) {
	float a = deg * 3.14159 / 180.;
	return vec2(v.x * cos(a) - v.y * sin(a), v.y * cos(a) + v.x * sin(a));
}", 
					["noise"] = ""
				};

				internal string Generate() {
					string GenWave(Wave wave) => $"{wave.Func}Wave({FloatString(wave.Base)}, {FloatString(wave.Amp)}, {FloatString(wave.Phase)}, {FloatString(wave.Freq)})";

					var cg = new CodeGen();
					
					cg.AddUniform("float", "uTime");

					var wavefuncs = new HashSet<string>();
					foreach(var layer in Layers) {
						if(layer.RgbGen is Gen.Wave cwave) wavefuncs.Add(cwave.Item.Func);
						if(layer.AlphaGen is Gen.Wave awave) wavefuncs.Add(awave.Item.Func);
						foreach(var mod in layer.TcMods)
							switch(mod) {
								case TcMod.Stretch stretch:
									wavefuncs.Add(stretch.Item.Func);
									break;
								case TcMod.Rotate _:
									wavefuncs.Add("rotate");
									break;
							}
					}
					wavefuncs.ForEach(func => cg.AddFunction(Prebuilts[func]));

					var samplerI = 0;

					foreach(var (i, layer) in Layers.Enumerate()) {
						var ln = $"layer{i}";
						string rgbMult = null, rgbGen = null, alphaGen = null;
						if(layer.Texture != null) {
							if(layer.TcGen.IsBase) {
								cg.AddVarying("vec2", "vTexCoord");
								cg.AddStmt(ln, "vec2 tc = vTexCoord;");
							} else if(layer.TcGen.IsLightmap) {
								cg.AddVarying("vec2", "vTexCoord");
								cg.AddStmt(ln, "vec2 tc = vTexCoord;");
							} else if(layer.TcGen.IsEnvironment) {
								cg.AddVarying("vec4", "vPosition");
								cg.AddVarying("vec3", "vNormal");
								cg.AddStmt(ln, "vec3 viewer = normalize(-vPosition.xyz);");
								cg.AddStmt(ln, "float d = dot(vNormal, viewer);");
								cg.AddStmt(ln, "vec3 reflected = vNormal*2.*d - viewer;");
								cg.AddStmt(ln, "vec2 tc = vec2(.5, .5) + reflected.xy * .5;");
							}

							foreach(var (j, mod) in layer.TcMods.Enumerate())
								switch(mod) {
									case TcMod.Rotate rotate: cg.AddStmt(ln, $"tc = rotate(tc, {FloatString(rotate.DegreesPerSecond)} * uTime)"); break;
									case TcMod.Scale scale: cg.AddStmt(ln, $"tc /= vec2({FloatString(scale.S)}, {FloatString(scale.T)});"); break;
									case TcMod.Scroll scroll:
										if(scroll.S == 1 && scroll.T == 1)
											cg.AddStmt(ln, "tc += uTime;");
										else
											cg.AddStmt(ln, $"tc += vec2({FloatString(scroll.S)}, {FloatString(scroll.T)}) * uTime;");
										break;
									case TcMod.Stretch stretch:
										cg.AddStmt(ln, $"float wave{j} = 1. / {GenWave(stretch.Item)};");
										cg.AddStmt(ln, $"tc = mat2(wave{j}, 0., 0., wave{j}) * tc + (0.5 - 0.5 * wave{j});");
										break;
									case TcMod.Transform transform:
										cg.AddStmt(ln, $"tc = mat2({FloatString(transform.M00)}, {FloatString(transform.M01)}, {FloatString(transform.M10)}, {FloatString(transform.M11)}) * tc + vec2({FloatString(transform.T0)}, {FloatString(transform.T1)});");
										break;
									case TcMod.Turb turb: break;
								}

							switch(layer.Texture) {
								case LayerCommand.AnimMap _:
								case LayerCommand.ClampMap _:
									cg.AddUniform("sampler2D", $"uTexSampler{samplerI}");
									rgbMult = $"texture2D(uTexSampler{samplerI++}, tc)";
									break;
								case LayerCommand.Map map:
									if(map.Item.IsLight) {
										cg.AddVarying("vec2", "vLmCoord");
										cg.AddUniform("sampler2D", "uLmSampler");
										rgbMult = "vec4(texture2D(uLmSampler, vLmCoord).xyz, 1.0)";
									} else {
										cg.AddUniform("sampler2D", $"uTexSampler{samplerI}");
										rgbMult = $"texture2D(uTexSampler{samplerI++}, tc)";
									}

									break;
							}

							switch(layer.RgbGen) {
								case Gen.ConstColor color:
									rgbGen = $"vec3({FloatString(color.Item1)}, {FloatString(color.Item2)}, {FloatString(color.Item3)})";
									break;
								case Gen.Wave wave:
									rgbGen = $"vec3({GenWave(wave.Item)})";
									break;
							}

							switch(layer.AlphaGen) {
								case Gen.ConstAlpha alpha:
									alphaGen = alpha.ToString();
									break;
								case Gen.Wave wave:
									alphaGen = GenWave(wave.Item);
									break;
							}
						}

						var colorElems = new List<string>();
						if(rgbMult != null)
							colorElems.Add(rgbMult);
						if(rgbGen != null || alphaGen != null)
							colorElems.Add($"vec4({rgbGen ?? "1., 1., 1."}, {alphaGen ?? "1."})");
						if(StackI == 0 && i == 0) {
							cg.AddVarying("vec2", "vLmCoord");
							cg.AddUniform("sampler2D", "uLmSampler");
							colorElems.Add("vec4(texture2D(uLmSampler, vLmCoord).xyz, 1.0)");
						}

						if(colorElems.Count == 0)
							colorElems.Add("vec4(1.0)");
						cg.AddStmt(ln, $"vec4 color = {colorElems.Join(" * ")};");
						if(layer.AlphaFunc != null) {
							if(layer.AlphaFunc.IsGT0) cg.AddStmt(ln, "if(color.a <= 0.) discard;");
							else if(layer.AlphaFunc.IsLT128) cg.AddStmt(ln, "if(color.a >= .5) discard;");
							else if(layer.AlphaFunc.IsGE128) cg.AddStmt(ln, "if(color.a < .9) discard;");
						}
						cg.AddStmt(ln, "return color;");
						cg.AddStmt("main", $"vec4 _{i} = clamp(layer{i}(), 0., 1.);");
					}

					string GenBlender(BlendFactor factor, string src, string dst) {
						if(factor.IsOne) return "1.";
						if(factor.IsZero) return "0.";
						if(factor.IsSrcAlpha) return $"{src}.a";
						if(factor.IsOneMinusSrcAlpha) return $"(1. - {src}.a)";
						if(factor.IsDstAlpha) return $"{dst}.a";
						if(factor.IsOneMinusDstAlpha) return $"(1. - {dst}.a)";
						if(factor.IsSrcColor) return src;
						if(factor.IsOneMinusSrcColor) return $"(1. - {src})";
						if(factor.IsDstColor) return dst;
						if(factor.IsOneMinusDstColor) return $"(1. - {dst})";
						return $"UnsupportedBlend{factor}";
					}

					cg.AddStmt("main", "vec4 cur = _0;");
					foreach(var (i, layer) in Layers.Enumerate().Skip(1))
						cg.AddStmt("main", $"cur = clamp(_{i} * {GenBlender(layer.BlendFunc.S, $"_{i}", "cur")} + cur * {GenBlender(layer.BlendFunc.D, $"_{i}", "cur")}, 0., 1.);");

					cg.AddStmt("main", "gl_FragColor = cur;");

					return cg.Generate();
				}
			}

			List<LayerStack> CoalesceStacks(List<LayerStack> stacks) {
				var fb = stacks.First().BlendFunc;
				if(!fb.Item1.IsOne || !fb.Item2.IsZero)
					return stacks;
				var ostacks = new List<LayerStack>();
				var fl = new List<Layer> {stacks.First().Layers.First()};
				foreach(var (i, stack) in stacks.Skip(1).Enumerate()) {
					if(stack.DepthWrite != null || stack.DepthFunc != null || (stack.BlendFunc.Item1.IsOne && stack.BlendFunc.Item2.IsZero))
						break;
					fl.Add(stack.Layers.First());
				}
				if(fl.Count == 1)
					return stacks;
				ostacks.Add(new LayerStack(0, fl.ToArray()));
				for(var i = fl.Count; i < stacks.Count; ++i)
					ostacks.Add(new LayerStack(i - fl.Count, stacks[i].Layers.ToArray()));
				return ostacks;
			}
			
			internal List<(string, DepthFunc, bool?, (BlendFactor, BlendFactor), List<LayerCommand>, bool)> Compile(List<List<string[]>> stages) {
				var lcstages = CodeToLayerCommands(stages);
				var stacks = lcstages.Enumerate().Select(t => {
					var (i, stage) = t;
					var layer = new Layer();
					foreach(var lc in stage) {
						switch(lc) {
							case LayerCommand.Map map: layer.Texture = map; break;
							case LayerCommand.ClampMap map: layer.Texture = map; break;
							case LayerCommand.AnimMap map: layer.Texture = map; break;
							
							case LayerCommand.TcGen gen: layer.TcGen = gen.Item; break;
							case LayerCommand.TcMod mod: layer.TcMods.Add(mod.Item); break;
							
							case LayerCommand.AlphaFunc func: layer.AlphaFunc = func.Item; break;
							case LayerCommand.BlendFunc blend: layer.BlendFunc = (blend.Src, blend.Dst); break;
							case LayerCommand.RgbGen gen: layer.RgbGen = gen.Item; break;
							case LayerCommand.AlphaGen gen: layer.AlphaGen = gen.Item; break;
							
							case LayerCommand.DepthFunc func: layer.DepthFunc = func.Item; break;
							case LayerCommand.DepthWrite dw: layer.DepthWrite = true; break;
						}
					}
					return new LayerStack(i, layer);
				}).ToList();
				if(stacks.Count == 0) return new List<(string, DepthFunc, bool?, (BlendFactor, BlendFactor), List<LayerCommand>, bool)>();
				stacks = CoalesceStacks(stacks);
				return stacks.Select(stack => (stack.Generate(), stack.DepthFunc, stack.DepthWrite, stack.BlendFunc, stack.Textures, stack.AlphaTested)).ToList();
			}

			List<List<LayerCommand>> CodeToLayerCommands(List<List<string[]>> stages) {
				Gen ParseGen(string[] line) {
					switch(line[0].ToLower()) {
						case "const":
							return line.Length == 2
								? Gen.NewConstAlpha(float.Parse(line[1])) 
								: Gen.NewConstColor(float.Parse(line[1]), float.Parse(line[2]), float.Parse(line[3]));
						case "entity" : return Gen.Entity;
						case "oneminusentity" : return Gen.OneMinusEntity;
						case "identity": return Gen.Identity;
						case "identitylighting": return Gen.IdentityLighting;
						case "lightingdiffuse": return Gen.LightingDiffuse;
						case "lightingspecular": return Gen.LightingSpecular;
						case "portal": return Gen.NewPortal(float.Parse(line[1]));
						case "vertex": return Gen.Vertex;
						case "exactvertex": return Gen.ExactVertex;
						case "wave": return Gen.NewWave(ParseWave(line.Skip(1).ToArray()));
						default:
							WriteLine($"Unhandled gen: {line.Join(" ")}");
							return null;
					}
				}

				BlendFactor ParseBlendFactor(string factor) {
					switch(factor.ToLower()) {
						case "gl_zero": return BlendFactor.Zero;
						case "gl_one": return BlendFactor.One;
						case "gl_src_color": return BlendFactor.SrcColor;
						case "gl_one_minus_src_color": return BlendFactor.OneMinusSrcColor;
						case "gl_dst_color": return BlendFactor.DstColor;
						case "gl_one_minus_dst_color": return BlendFactor.OneMinusDstColor;
						case "gl_src_alpha": return BlendFactor.SrcAlpha;
						case "gl_one_minus_src_alpha": return BlendFactor.OneMinusSrcAlpha;
						case "gl_dst_alpha": return BlendFactor.DstAlpha;
						case "gl_one_minus_dst_alpha": return BlendFactor.OneMinusDstAlpha;
						case "gl_constant_color": return BlendFactor.ConstantColor;
						case "gl_one_minus_constant_color": return BlendFactor.OneMinusConstantColor;
						case "gl_constant_alpha": return BlendFactor.ConstantAlpha;
						case "gl_one_minus_constant_alpha": return BlendFactor.OneMinusConstantAlpha;
						default: throw new ArgumentException();
					}
				}

				Wave ParseWave(string[] line) => 
					new Wave(line[0].ToLower(), float.Parse(line[1]), float.Parse(line[2]), float.Parse(line[3]), float.Parse(line[4]));

				return stages.Skip(1).Select(
					stage => stage.Select(line => {
						switch(line[0].ToLower()) {
							case "alphafunc":
								switch(line[1].ToLower()) {
									case "gt0": return LayerCommand.NewAlphaFunc(AlphaFunc.GT0);
									case "lt128": return LayerCommand.NewAlphaFunc(AlphaFunc.LT128);
									case "ge128": return LayerCommand.NewAlphaFunc(AlphaFunc.GE128);
									default:
										WriteLine($"Unhandled alphafunc: {line.Skip(1).Join(" ")}");
										return null;
								}
							case "alphagen":
								return LayerCommand.NewAlphaGen(ParseGen(line.Skip(1).ToArray()));
							case "animmap":
								return LayerCommand.NewAnimMap(float.Parse(line[1]), line.Skip(2).ToArray());
							case "blendfunc":
								if(line.Length == 3) return LayerCommand.NewBlendFunc(ParseBlendFactor(line[1]), ParseBlendFactor(line[2]));
								
								switch(line[1].ToLower()) {
									case "add": case "gl_add":
										return LayerCommand.NewBlendFunc(BlendFactor.One, BlendFactor.One);
									case "filter":
										return LayerCommand.NewBlendFunc(BlendFactor.DstColor, BlendFactor.Zero);
									case "blend":
										return LayerCommand.NewBlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
									default:
										WriteLine($"Unhandled blendfunc: {line.Skip(1).Join(" ")}");
										return null;
								}
							case "clampmap":
								return LayerCommand.NewClampMap(line[1]);
							case "depthfunc":
								switch(line[1].ToLower()) {
									case "lequal": return LayerCommand.NewDepthFunc(DepthFunc.LEqual);
									case "equal": return LayerCommand.NewDepthFunc(DepthFunc.Equal);
									default: 
										WriteLine($"Unhandled depthfunc: {line.Skip(1).Join(" ")}");
										return null;
								}
							case "depthwrite":
								return LayerCommand.NewDepthWrite(true);
							case "fogparms":
								return LayerCommand.NewFogParms(float.Parse(line[1]));
							case "rgbgen":
								return LayerCommand.NewRgbGen(ParseGen(line.Skip(1).ToArray()));
							case "map":
								switch(line[1].ToLower()) {
									case "$lightmap":
									case "$lightmapt":
										return LayerCommand.NewMap(Map.Light);
									case "$whiteimage":
									case "*white":
										return LayerCommand.NewMap(Map.White);
									default:
										return LayerCommand.NewMap(Map.NewTexture(line[1]));
								}
							case "tcgen":
								switch(line[1].ToLower()) {
									case "base": return LayerCommand.NewTcGen(TcGen.Base);
									case "lightmap": return LayerCommand.NewTcGen(TcGen.Lightmap);
									case "environment": return LayerCommand.NewTcGen(TcGen.Environment);
									case "vector":
										return LayerCommand.NewTcGen(TcGen.NewVector(
											float.Parse(line[3]), float.Parse(line[4]), float.Parse(line[5]), 
											float.Parse(line[8]), float.Parse(line[9]), float.Parse(line[10])
										));
									default:
										WriteLine($"Unhandled tcgen type: {line.Skip(1).Join(" ")}");
										return null;
								}
							case "tcmod":
								switch(line[1].ToLower()) {
									case "rotate": return LayerCommand.NewTcMod(TcMod.NewRotate(float.Parse(line[2])));
									case "scale": return LayerCommand.NewTcMod(TcMod.NewScale(float.Parse(line[2]), float.Parse(line[3])));
									case "scroll": return LayerCommand.NewTcMod(TcMod.NewScroll(float.Parse(line[2]), float.Parse(line[3])));
									case "stretch":
										return LayerCommand.NewTcMod(TcMod.NewStretch(ParseWave(line.Skip(2).ToArray())));
									case "transform":
										return LayerCommand.NewTcMod(TcMod.NewTransform(
											float.Parse(line[2]), float.Parse(line[3]), 
											float.Parse(line[4]), float.Parse(line[5]),
											float.Parse(line[6]), float.Parse(line[7])
										));
									case "turb":
										if(line.Length != 6) return null;
										return LayerCommand.NewTcMod(TcMod.NewTurb(float.Parse(line[2]), float.Parse(line[3]), float.Parse(line[4]),
											float.Parse(line[5])));
									default:
										WriteLine($"Unhandled tcmod type: {line.Skip(1).Join(" ")}");
										return null;
								}
							case "alphamap": case "detail": case "surfaceparm":
								return null;
							default:
								WriteLine($"Unhandled layer command string: {line[0]}");
								return null;
						}
					}).Where(x => x != null).ToList()
				).ToList();
			}
		}
		
		static Dictionary<string, List<List<string[]>>> ParseShaders(string code) {
			code = code.Replace("\r", "\n");
			code = Regex.Replace(code, "//.*?\n$", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = code.Replace("{", "\n{\n");
			code = Regex.Replace(code, @"^\s+", "", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, @"\s+$", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, "\n[\n \t]*", "\n", RegexOptions.Singleline | RegexOptions.Multiline);
			code = Regex.Replace(code, @"[ \t]+", " ", RegexOptions.Singleline | RegexOptions.Multiline);
			var data = code.Trim().Split('\n');

			var inside = false;
			List<string[]> stage = null;
			var instage = false;
			string ntex = null;
			var odict = new Dictionary<string, List<List<string[]>>>();
			for(var i = 0; i < data.Length; ++i) {
				if(data[i] == "") continue;
				if(!inside) {
					ntex = data[i++];
					Debug.Assert(data[i] == "{");
					odict[ntex] = new List<List<string[]>>();
					odict[ntex].Add(stage = new List<string[]>());
					inside = true;
				} else if(data[i] == "{") {
					odict[ntex].Add(stage = new List<string[]>());
					instage = true;
				} else if(data[i] == "}") {
					if(instage)
						instage = false;
					else
						inside = false;
				} else
					stage.Add(data[i].Split(' '));
			}
			
			return odict;
		}

		static int BlendMap(BlendFactor fac) {
			if(fac.IsConstantAlpha) return 0x8003;
			if(fac.IsConstantColor) return 0x8002;
			if(fac.IsDstAlpha) return 0x0304;
			if(fac.IsDstColor) return 0x0306;
			if(fac.IsOne) return 1;
			if(fac.IsOneMinusConstantAlpha) return 0x8004;
			if(fac.IsOneMinusConstantColor) return 0x8002;
			if(fac.IsOneMinusDstAlpha) return 0x0305;
			if(fac.IsOneMinusDstColor) return 0x0307;
			if(fac.IsOneMinusSrcAlpha) return 0x0303;
			if(fac.IsOneMinusSrcColor) return 0x0301;
			if(fac.IsSrcAlpha) return 0x0302;
			if(fac.IsSrcColor) return 0x0300;
			return 0;
		}
		
		public static Dictionary<string, JLayer[]> Convert(byte[] istream) {
			var parsed = ParseShaders(Encoding.ASCII.GetString(istream));
			parsed["plaintexture"] = new List<List<string[]>>();
			parsed["plaintexture"].Add(new List<string[]>());
			parsed["plaintexture"].Add(new List<string[]>());
			parsed["plaintexture"][1].Add(new []{"map", "sometexture"});
			return parsed.Select(x => {
				var s = new Compiler().Compile(x.Value);
				return (x.Key, s.Select(l => new JLayer {
					FragShader = l.Item1,
					Blend = new[] {BlendMap(l.Item4.Item1), BlendMap(l.Item4.Item2)},
					Textures = l.Item5.Select(t => {
						switch(t) {
							case LayerCommand.Map map:
								switch(map.Item) {
									case Map.Texture tex:
										return new JTexture {Texture = tex.Name};
									default:
										return null;
								}
							case LayerCommand.ClampMap clamp:
								return new JTexture {Texture = clamp.Name, Clamp = true};
							case LayerCommand.AnimMap ani:
								return new JTexture {AnimTex = ani.Names, Frequency = ani.Freq};
							default:
								return null;
						}
					}).Where(t => t != null).ToArray(), 
					DepthWrite = l.Item3, 
					AlphaTested = l.Item6
				}).ToArray());
			}).ToDictionary();
		}
	}
}