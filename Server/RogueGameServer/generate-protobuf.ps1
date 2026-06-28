param(
    [string]$ProtobufHome = "E:\protobuf"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$protoc = Join-Path $ProtobufHome "bin\protoc.exe"
$include = Join-Path $ProtobufHome "include"
$protoDir = Join-Path $root "Assets\Proto"
$outDir = Join-Path $root "Assets\Scripts\RunTime\Server\Protobuf\Generated"
$proto = Join-Path $protoDir "network_packet_runtime.proto"

& $protoc `
    "--proto_path=$protoDir" `
    "--proto_path=$include" `
    "--csharp_out=$outDir" `
    $proto
