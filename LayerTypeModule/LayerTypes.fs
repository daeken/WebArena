namespace Converter

type Map = 
    | White
    | Light
    | Texture of Name : string

type BlendFactor = 
    | Zero
    | One
    | SrcColor
    | OneMinusSrcColor
    | DstColor
    | OneMinusDstColor
    | SrcAlpha
    | OneMinusSrcAlpha
    | DstAlpha
    | OneMinusDstAlpha
    | ConstantColor
    | OneMinusConstantColor
    | ConstantAlpha
    | OneMinusConstantAlpha

type Wave = {Func : string; Base : float; Amp : float; Phase : float; Freq : float}

type Gen = 
    | ConstColor of float * float * float
    | ConstAlpha of float
    | Identity
    | IdentityLighting
    | Vertex
    | ExactVertex
    | Entity
    | OneMinusEntity
    | LightingDiffuse
    | LightingSpecular
    | Wave of Wave
    | Portal of Range : float

type TcGen = 
    | Base
    | Lightmap
    | Environment
    | Vector of Sx : float * Sy : float * Sz : float * Tx : float * Ty : float * Tz : float

type TcMod = 
    | Rotate of DegreesPerSecond : float
    | Scale of S : float * T : float
    | Scroll of S : float * T : float
    | Stretch of Wave
    | Transform of M00 : float * M01 : float * M10 : float * M11 : float * T0 : float * T1 : float
    | Turb of Base : float * Amp : float * Phase : float * Freq : float

type DepthFunc = 
    | LEqual
    | Equal

type AlphaFunc = 
    | GT0
    | LT128
    | GE128

type LayerCommand = 
    | Map of Map
    | ClampMap of Name : string
    | AnimMap of Freq : float * Names : string[]
    | BlendFunc of Src : BlendFactor * Dst : BlendFactor
    | FogParms of Depth : float
    | RgbGen of Gen
    | AlphaGen of Gen
    | TcGen of TcGen
    | TcMod of TcMod
    | DepthFunc of DepthFunc
    | DepthWrite of bool
    | AlphaFunc of AlphaFunc
