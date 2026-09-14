# Development shell for GleamFarmer.
#
#   nix-shell            # enter the dev env
#
# Provides the .NET SDK (to build the plugin + transpiler and run ilspycmd),
# plus tooling needed for the WASM spike.

{ pkgs ? import <nixpkgs> {} }:

let
  dotnet = pkgs.dotnet-sdk;
in
pkgs.mkShell {
  packages = [
    dotnet
    pkgs.gleam          # local Gleam for parity tests against the embedded compiler
    pkgs.erlang         # optional: inspect Gleam's Erlang target
    pkgs.nodejs         # cross-check Gleam-compiled JS outside Jint
    pkgs.git
  ];

  shellHook = ''
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export DOTNET_NOLOGO=1
    # Ensure dotnet global tools (e.g. ilspycmd) are on PATH.
    export PATH="$HOME/.dotnet/tools:$PATH"
    echo "GleamFarmer dev shell ready."
  '';
}