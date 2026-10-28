using OptiCore.Models;
using OptiCore.Services;

int pass = 0, fail = 0;
void Check(bool ok, string label, string detail = "")
{
    if (ok) { pass++; return; }
    fail++;
    Console.WriteLine($"  FAIL  {label}  {detail}");
}

// ───────────────────────────────────────────── 1. Socket
Console.WriteLine("== 1. Socket (SMBIOS primeiro, nome como fallback) ==");
var sockets = new (string Model, string Smbios, string Expect)[]
{
    ("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", "AM5", "AM5"),
    ("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", "",    "AM5"),   // fallback por nome: era o bug (dava AM4)
    ("AMD Ryzen 5 8600G w/ Radeon 760M Graphics", "",    "AM5"),
    ("AMD Ryzen 5 8500G w/ Radeon 740M Graphics", "",    "AM5"),
    ("AMD Ryzen 3 8300G w/ Radeon 740M Graphics", "",    "AM5"),
    ("AMD Ryzen 7 7800X3D 8-Core Processor",      "",    "AM5"),
    ("AMD Ryzen 5 7600 6-Core Processor",         "",    "AM5"),
    ("AMD Ryzen 7 9700X 8-Core Processor",        "",    "AM5"),
    ("AMD Ryzen 9 9950X3D2 16-Core Processor",    "AM5", "AM5"),
    ("AMD Ryzen 5 5600G with Radeon Graphics",    "AM4", "AM4"),
    ("AMD Ryzen 5 5600G with Radeon Graphics",    "",    "AM4"),
    ("AMD Ryzen 9 5900X 12-Core Processor",       "",    "AM4"),
    ("AMD Ryzen 7 5800X3D 8-Core Processor",      "",    "AM4"),
    ("AMD Ryzen 7 3700X 8-Core Processor",        "",    "AM4"),
    ("AMD Ryzen 5 2600 Six-Core Processor",       "",    "AM4"),
    ("AMD Ryzen 7 7840HS w/ Radeon 780M Graphics","FP7", "Unknown"), // notebook: fora do escopo AM4/AM5
    ("Intel(R) Core(TM) i7-13700K",               "LGA1700", "Unknown"),
    ("AMD EPYC 9654 96-Core Processor",           "SP5", "Unknown"),
};
foreach (var s in sockets)
{
    var got = HardwareClassifier.InferSocket(s.Model, s.Smbios);
    Check(got == s.Expect, $"socket '{s.Model}' smbios='{s.Smbios}'", $"esperado {s.Expect}, obtido {got}");
}

// ───────────────────────────────────────────── 2. APU
Console.WriteLine("== 2. APU ==");
foreach (var m in new[] { "AMD Ryzen 7 8700G w/ Radeon 780M Graphics", "AMD Ryzen 5 5600G with Radeon Graphics", "AMD Ryzen 5 5600GT 6-Core Processor" })
    Check(HardwareClassifier.IsApu(m), $"APU '{m}'", "deveria ser APU");
foreach (var m in new[] { "AMD Ryzen 9 9950X3D2 16-Core Processor", "AMD Ryzen 7 7800X3D 8-Core Processor", "AMD Ryzen 9 5900X 12-Core Processor", "AMD Ryzen 5 7600 6-Core Processor" })
    Check(!HardwareClassifier.IsApu(m), $"nao-APU '{m}'", "nao deveria ser APU");

// ───────────────────────────────────────────── 3. GPU
Console.WriteLine("== 3. Classificacao de GPU ==");
var gpus = new (string Name, string Pnp, GpuClass Expect)[]
{
    ("NVIDIA GeForce RTX 5090",            @"PCI\VEN_10DE&DEV_2B85", GpuClass.NvidiaDiscrete),
    ("NVIDIA GeForce GTX 1660 SUPER",      @"PCI\VEN_10DE&DEV_21C4", GpuClass.NvidiaDiscrete),
    ("AMD Radeon(TM) 780M Graphics",       @"PCI\VEN_1002&DEV_15BF", GpuClass.Integrated),      // iGPU do 8700G
    ("AMD Radeon(TM) Graphics",            @"PCI\VEN_1002&DEV_164E", GpuClass.Integrated),      // iGPU Zen 3/4 generica
    ("AMD Radeon(TM) Vega 8 Graphics",     @"PCI\VEN_1002&DEV_1636", GpuClass.Integrated),      // APUs AM4
    ("AMD Radeon(TM) 610M",                @"PCI\VEN_1002&DEV_164E", GpuClass.Integrated),
    ("AMD Radeon RX 7800 XT",              @"PCI\VEN_1002&DEV_747E", GpuClass.AmdDiscrete),
    ("AMD Radeon RX 6600",                 @"PCI\VEN_1002&DEV_73FF", GpuClass.AmdDiscrete),
    ("Radeon RX 580 Series",               @"PCI\VEN_1002&DEV_67DF", GpuClass.AmdDiscrete),
    ("AMD Radeon RX Vega 64",              "",                        GpuClass.AmdDiscrete),      // sem PNP: vendor pelo nome
    ("AMD Radeon PRO W7800",               @"PCI\VEN_1002&DEV_7448", GpuClass.AmdDiscrete),
    ("AMD Radeon VII",                     @"PCI\VEN_1002&DEV_66AF", GpuClass.AmdDiscrete),
    ("Intel(R) UHD Graphics 770",          @"PCI\VEN_8086&DEV_4680", GpuClass.Integrated),
    ("Intel(R) Arc(TM) A770 Graphics",     @"PCI\VEN_8086&DEV_56A0", GpuClass.IntelDiscrete),
    ("Microsoft Basic Display Adapter",    "",                        GpuClass.Unknown),
};
foreach (var g in gpus)
{
    var got = HardwareClassifier.ClassifyGpu(g.Name, g.Pnp);
    Check(got == g.Expect, $"gpu '{g.Name}'", $"esperado {g.Expect}, obtido {got}");
}
GpuClass Best(params GpuClass[] c) => c.OrderByDescending(HardwareClassifier.Rank).First();
Check(Best(GpuClass.Integrated, GpuClass.NvidiaDiscrete) == GpuClass.NvidiaDiscrete, "ranking iGPU + NVIDIA -> NVIDIA");
Check(Best(GpuClass.Integrated, GpuClass.AmdDiscrete) == GpuClass.AmdDiscrete, "ranking iGPU + AMD dGPU -> AMD dGPU");
Check(Best(GpuClass.Integrated) == GpuClass.Integrated, "ranking so iGPU -> iGPU");

// ───────────────────────────────────────────── 4. Boost de fabrica / falso positivo de OC
Console.WriteLine("== 4. Tabela de boost e sugestao de OC ==");
var boosts = new (string Model, double Base, double Expect)[]
{
    ("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", 4200, 5100),
    ("AMD Ryzen 5 8600G w/ Radeon 760M Graphics", 4300, 5000),
    ("AMD Ryzen 5 8500G w/ Radeon 740M Graphics", 3500, 5000),
    ("AMD Ryzen 3 8300G w/ Radeon 740M Graphics", 3400, 4900),
};
foreach (var b in boosts)
{
    var got = HardwareDetectionService.GetFactoryBoostMhz(b.Model, b.Base);
    Check(Math.Abs(got - b.Expect) < 1, $"boost '{b.Model}'", $"esperado {b.Expect}, obtido {got}");
    // O chip em stock sobe ate o boost de fabrica: NAO pode parecer overclock (regra do app: current > boost*1.05)
    Check(!(b.Expect > got * 1.05), $"falso positivo de OC '{b.Model}'");
}
// Modelo fora da tabela (futuro): stock em ~1.39x da base nao pode virar "FixedOC"
var unk = HardwareDetectionService.GetFactoryBoostMhz("AMD Ryzen 9 9999X Futuro", 4300);
Check(4300 * 1.39 <= unk * 1.05, "modelo desconhecido: stock a 1.39x da base nao parece OC", $"limite {unk * 1.05:N0}");

// ───────────────────────────────────────────── 5. Topologia (layouts simulados)
Console.WriteLine("== 5. Topologia ==");
CpuTopology Topo(params int[] l3Mb)
{
    var list = new List<L3Domain>();
    for (int i = 0; i < l3Mb.Length; i++)
        list.Add(new L3Domain { SizeKb = l3Mb[i] * 1024, Mask = 0xFFFFUL << (16 * i) });
    return new CpuTopology { Valid = true, L1DataKb = 48, L2Kb = 1024, L3 = list };
}
HardwareProfile Cpu(string model, int family, int cores, bool x3d) =>
    new() { CpuModel = model, CpuZenFamily = family, CpuPhysicalCores = cores, CpuIsX3D = x3d };

void Layout(string label, HardwareProfile p, CpuTopology t, int ccd, int cacheIdx, bool sym)
{
    HardwareDetectionService.ApplyMeasuredTopology(p, t);
    Check(p.CpuCcdCount == ccd, $"{label}: CCDs", $"esperado {ccd}, obtido {p.CpuCcdCount}");
    Check(p.CacheCcdIndex == cacheIdx, $"{label}: CacheCcdIndex", $"esperado {cacheIdx}, obtido {p.CacheCcdIndex}");
    Check(p.CpuVCacheSymmetric == sym, $"{label}: simetrico", $"esperado {sym}, obtido {p.CpuVCacheSymmetric}");
}
Layout("9950X3D2 (96/96)",  Cpu("AMD Ryzen 9 9950X3D2 16-Core Processor", 26, 16, true),  Topo(96, 96), 2, 0, true);
Layout("9950X3D (96/32)",   Cpu("AMD Ryzen 9 9950X3D 16-Core Processor", 26, 16, true),   Topo(96, 32), 2, 0, false);
Layout("X3D com cache no CCD1 (32/96)", Cpu("AMD Ryzen 9 7950X3D 16-Core Processor", 25, 16, true), Topo(32, 96), 2, 1, false);
Layout("9950X (32/32)",     Cpu("AMD Ryzen 9 9950X 16-Core Processor", 26, 16, false),    Topo(32, 32), 2, -1, false);
Layout("9800X3D (96)",      Cpu("AMD Ryzen 7 9800X3D 8-Core Processor", 26, 8, true),     Topo(96),     1, -1, false);
Layout("8700G (16, sem V-Cache)", Cpu("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", 25, 8, false), Topo(16), 1, -1, false);
Layout("5900X (32/32)",     Cpu("AMD Ryzen 9 5900X 12-Core Processor", 25, 12, false),    Topo(32, 32), 2, -1, false);
Layout("3900X (4 x 16 por CCX)", Cpu("AMD Ryzen 9 3900X 12-Core Processor", 23, 12, false), Topo(16, 16, 16, 16), 2, -1, false);
Layout("3700X (2 x 16 por CCX)", Cpu("AMD Ryzen 7 3700X 8-Core Processor", 23, 8, false),  Topo(16, 16), 1, -1, false);
var invalid = Cpu("AMD Ryzen 9 9950X3D2 16-Core Processor", 26, 16, true);
invalid.CpuCcdCount = 7;
HardwareDetectionService.ApplyMeasuredTopology(invalid, new CpuTopology { Valid = false });
Check(invalid.CpuCcdCount == 7, "topologia invalida nao altera o perfil (fallback preservado)");

// ───────────────────────────────────────────── 6. Motor de decisao: itens de GPU por cenario
Console.WriteLine("== 6. Motor de decisao ==");
HardwareProfile Prof(string cpu, int fam, int cores, string gpuName, string pnp, string socket)
{
    var kind = HardwareClassifier.ClassifyGpu(gpuName, pnp);
    var vid = HardwareClassifier.VendorId(gpuName, pnp);
    var p = new HardwareProfile
    {
        CpuModel = cpu, CpuManufacturer = "AMD", CpuSocket = socket, CpuIsAM5 = socket == "AM5", CpuIsAM4 = socket == "AM4",
        CpuPhysicalCores = cores, CpuLogicalCores = cores * 2, CpuZenFamily = fam, CpuIsZen = true,
        CpuCompatStatus = CompatibilityStatus.Supported, CpuCompatReason = "ok",
        GpuModel = gpuName, GpuKind = kind.ToString(), GpuPciVendorId = vid,
        GpuManufacturer = vid == "10DE" ? "NVIDIA" : vid == "1002" ? "AMD" : "Intel",
        GpuIsIntegrated = kind == GpuClass.Integrated, HasDedicatedGpu = kind != GpuClass.Integrated,
        GpuCompatStatus = CompatibilityStatus.Supported, GpuCompatReason = "ok",
    };
    if (vid == "10DE") { p.GpuRtxGeneration = 4; p.GpuIsRtx30Plus = true; p.GpuIsNvidiaRtx = true; }
    return p;
}
var engine = new DecisionEngineService();
void Scenario(string label, HardwareProfile p, Action<List<OptimizationItem>> extra)
{
    var items = engine.GenerateOptimizations(p);
    Console.WriteLine($"  [{label}]  total={items.Count}");
    foreach (var it in items.Where(i => i.Category == "GPU"))
        Console.WriteLine($"      {it.Id,-22} aplicavel={it.IsApplicable,-5} {(it.IsApplicable ? "" : "motivo: " + it.SkipReason)}");
    Check(!items.Any(i => i.Id == "gpu_interrupt_affinity"), $"{label}: gpu_interrupt_affinity removido");
    extra(items);
}
OptimizationItem? Get(List<OptimizationItem> l, string id) => l.FirstOrDefault(i => i.Id == id);

Scenario("8700G so com iGPU", Prof("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", 25, 8, "AMD Radeon(TM) 780M Graphics", @"PCI\VEN_1002&DEV_15BF", "AM5"), l =>
{
    Check(Get(l, "gpu_msi_vectors") == null, "8700G iGPU: sem item de MSI de GPU");
    Check(Get(l, "hags") == null, "8700G iGPU: sem HAGS (iGPU nao validada)");
    Check(l.Count > 60, "8700G iGPU: otimizacoes de CPU/OS/rede/servicos continuam disponiveis", $"so {l.Count}");
});
Scenario("8700G + RTX 4060", Prof("AMD Ryzen 7 8700G w/ Radeon 780M Graphics", 25, 8, "NVIDIA GeForce RTX 4060", @"PCI\VEN_10DE&DEV_2882", "AM5"), l =>
{
    Check(Get(l, "gpu_msi_vectors")?.IsApplicable == true, "8700G + RTX: MSI aplicavel");
    Check(Get(l, "nvidia_perf_level") != null, "8700G + RTX: itens NVIDIA presentes");
});
Scenario("5600X + RX 7800 XT", Prof("AMD Ryzen 5 5600X 6-Core Processor", 25, 6, "AMD Radeon RX 7800 XT", @"PCI\VEN_1002&DEV_747E", "AM4"), l =>
{
    Check(Get(l, "gpu_msi_vectors")?.IsApplicable == false, "RX 7800 XT: MSI marcado como nao aplicavel (so NVIDIA)");
    Check(!string.IsNullOrEmpty(Get(l, "gpu_msi_vectors")?.SkipReason), "RX 7800 XT: MSI explica o motivo");
    Check(Get(l, "hags")?.IsApplicable == true, "RX 7800 XT: HAGS aplicavel (vendor-neutral)");
    Check(Get(l, "nvidia_perf_level") == null, "RX 7800 XT: sem itens NVIDIA");
});
Scenario("9950X3D2 + RTX 5090", Prof("AMD Ryzen 9 9950X3D2 16-Core Processor", 26, 16, "NVIDIA GeForce RTX 5090", @"PCI\VEN_10DE&DEV_2B85", "AM5"), l =>
{
    Check(Get(l, "gpu_msi_vectors")?.IsApplicable == true, "RTX 5090: MSI aplicavel");
});

// ───────────────────────────────────────────── 7. Deteccao real nesta maquina (somente leitura)
Console.WriteLine("== 7. Deteccao REAL nesta maquina (9950X3D2 + RTX 5090) ==");
var real = new HardwareDetectionService().RunDetection();
Console.WriteLine($"  CPU     : {real.CpuModel}");
Console.WriteLine($"  Socket  : {real.CpuSocket} | APU={real.CpuIsApu} | X3D={real.CpuIsX3D} | CCDs={real.CpuCcdCount} | L3 por dominio (MB)=[{string.Join(", ", real.CpuL3DomainsMb)}]");
Console.WriteLine($"  V-Cache : CacheCcdIndex={real.CacheCcdIndex} | simetrico={real.CpuVCacheSymmetric} | CCD preferido={real.CpuPreferredCcd}");
Console.WriteLine($"  GPU     : {real.GpuModel} | tipo={real.GpuKind} | fab={real.GpuManufacturer} | dedicada={real.HasDedicatedGpu} | integrada={real.GpuIsIntegrated}");
Console.WriteLine($"  Gate    : CPU={real.CpuCompatStatus} | GPU={real.GpuCompatStatus} | IsSupportedGpu={real.IsSupportedGpu}");
Console.WriteLine($"  OC      : sugerido={real.OcModeSuggested} ({real.OcModeSuggestionReason})");
Check(real.CpuSocket == "AM5", "real: socket AM5");
Check(real.CpuCcdCount == 2, "real: 2 CCDs");
Check(real.CpuIsX3D && real.CpuVCacheSymmetric && real.CacheCcdIndex == 0, "real: 9950X3D2 reconhecido como V-Cache simetrico");
Check(real.CpuL3DomainsMb.Length == 2 && real.CpuL3DomainsMb.All(x => x == 96), "real: L3 medido 96/96 MB");
Check(real.GpuKind == nameof(GpuClass.NvidiaDiscrete) && real.GpuIsNvidiaRtx, "real: RTX 5090 = NVIDIA dedicada RTX 30+");
Check(real.GpuCompatStatus == CompatibilityStatus.Supported && real.IsSupportedGpu, "real: gate de GPU = Supported");

Console.WriteLine();
Console.WriteLine($"RESULTADO: {pass} passaram, {fail} falharam");
return fail == 0 ? 0 : 1;
