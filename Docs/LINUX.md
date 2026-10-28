# Linux x86_64

O port usa Unity **6000.6.4f1**, backend Mono e URP 17.6.0. Inclui as três cenas, começando pela expansão Praça–Ifes–FAESA, os mesmos perfis, editor físico, histórico, persistência e dados cartográficos locais.

Na versão 0.3.0, clique numa bolinha azul para abrir **Conhecer o nó**. O [guia de inspeção de nós](INSPECAO_DE_NOS.md) explica o painel e o comando **Localizar alteração do cenário**. O comportamento é o mesmo do Windows.

**Validado em Ubuntu 22.04.5:** 60/60 combinações aprovadas, com renderização gráfica via Xvfb/Mesa. Consulte [os resultados e limites](VALIDACAO.md) e a [captura da aproximação da FAESA](Resultados/Linux/Navigation-FAESA-Closeup.png).

## Executar o pacote

Baixe `TrajetoInclusivo-Linux-x86_64.tar.gz` e `SHA256SUMS.txt` na [página de versões](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/releases). O pacote é nativo; não utiliza Wine e não exige instalar o Editor Unity.

```sh
sha256sum -c SHA256SUMS.txt
tar -xzf TrajetoInclusivo-Linux-x86_64.tar.gz
cd TrajetoInclusivo-Linux-x86_64
./TrajetoInclusivo.sh
```

Mantenha o executável, `UnityPlayer.so`, `MonoBleedingEdge`, a pasta `_Data`, `MapData` e `Documentacao` juntos. O arquivo tar preserva as permissões executáveis. Se a ferramenta de extração removê-las:

```sh
chmod +x TrajetoInclusivo.sh Testar-Linux.sh TrajetoInclusivo.x86_64
```

O alvo é desktop Linux **x86_64**. A Unity documenta suporte a Ubuntu 22.04/24.04, CPU x64 com SSE2 e GPU compatível com OpenGL 3.2+ ou Vulkan; consulte os [requisitos oficiais](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html). Este projeto configura OpenGL Core como primeira opção e Vulkan como alternativa. Se necessário, escolha explicitamente:

```sh
./TrajetoInclusivo.sh -force-glcore
./TrajetoInclusivo.sh -force-vulkan
```

Uma execução de QA com renderização por software não substitui um teste dos drivers e desempenho da máquina de destino. Não há pacote ARM64 nesta entrega.

## Controles e dados

Os controles são os mesmos descritos no [guia da interface](INTERFACE_E_VALIDACAO.md): WASD, Q/E, botão direito, roda, T e Home; os botões **Aproximar** localizam Praça, Ifes e FAESA. A comparação acessível exige ativar explicitamente as hipóteses de teste. A referência da FAESA continua aproximada e não identifica uma entrada verificada.

Para importar CSV, use um caminho Linux, por exemplo `/home/usuario/Dados/cotas.csv`. Nomes de arquivos diferenciam maiúsculas e minúsculas. O formato, referência horizontal, datum e limites continuam os de [edição e relevo](EDICAO_E_RELEVO.md).

As experiências são gravadas em `Application.persistentDataPath`. No Linux, a base normalmente é `~/.config/unity3d/Trajeto Inclusivo/Trajeto Inclusivo — Avenida Vitória/`, ou a localização derivada de `XDG_CONFIG_HOME`; a barra de status mostra o caminho efetivo. Veja a [referência oficial da API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html). Cada cena usa seu próprio `<cena>.experiment.json`. Experiências de Windows podem ser copiadas se a identidade do mapa/grafo for a mesma; a carga valida a compatibilidade antes de alterar a cena.

## Gerar a partir do projeto

Clone o repositório e abra sua raiz no Unity Hub. Use a versão de `ProjectSettings/ProjectVersion.txt` e instale **Linux Build Support (Mono)**. A [referência do Hub CLI](https://docs.unity.com/en-us/hub/hub-cli-reference) identifica o módulo como `linux-mono`.

No Editor, use **Trajeto Inclusivo → Gerar executável Linux x86_64**. Pelo terminal, com `UNITY_EDITOR` apontando para o binário do Editor:

```sh
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -buildTarget Linux64 -executeMethod TrajetoInclusivo.Editor.LinuxBuild.Build \
  -quit -logFile "$PWD/linux-build.log"
```

O resultado fica em `Builds/Linux`. O mesmo método pode fazer a compilação cruzada no Editor Windows com o módulo instalado. `DesktopBuild` compartilha preparação das malhas, seleção de cenas e cópia de documentação/créditos entre Windows e Linux. `WindowsBuild.Build` continua disponível.

Para criar o tar a partir de Windows/PowerShell 7.4+:

```powershell
./Tools/Package-Linux.ps1
```

Para criar um pacote depois de gerar no Linux, use `tar` preservando permissões; marque como executáveis o player, os dois scripts e o crash handler quando presente.

## Testar no Linux

O pacote inclui a mesma matriz de QA usada no Windows: 60 combinações nas três cenas, incluindo Praça–FAESA e Ifes–FAESA, seleção de pontos, aproximação e detecção de erros. A execução encerra automaticamente.

```sh
./Testar-Linux.sh /tmp/trajeto-inclusivo-resultados
```

Em ambiente sem monitor, com Xvfb e Mesa instalados:

```sh
LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a -s '-screen 0 1600x1000x24' \
  ./Testar-Linux.sh /tmp/trajeto-inclusivo-resultados
```

O resultado esperado é `LINUX_NATIVE_QA_OK`, 60 linhas `PASS` em `Stage4-Player-Matrix.csv` e zero erros em `Stage4-Player.txt`. Logs e capturas ficam no diretório informado. Não use `-nographics` nesse teste visual, pois a rotina precisa de frames renderizados.

O workflow `linux-smoke.yml` baixa o pacote de uma release e executa esse teste em Ubuntu 22.04 com Xvfb/Mesa, sem depender de licença do Editor. Os artefatos da execução registram o resultado real. Gerar um ELF no Windows, por si só, não comprova sua execução no Linux; o estado da validação está em [VALIDACAO.md](VALIDACAO.md).

## Documentação incluída

- [README](../README.md): instalação e operação do protótipo.
- [Arquitetura](ARQUITETURA.md): módulos, custos e atualização.
- [Dados geográficos](DADOS_GEOGRAFICOS.md): origem, projeção e limites.
- [Expansão FAESA](EXPANSAO_FAESA.md): snapshot adicional e referência aproximada.
- [Edição e relevo](EDICAO_E_RELEVO.md): obstáculos, CSV, histórico e persistência.
- [Interface e validação](INTERFACE_E_VALIDACAO.md): fluxos e critérios de sucesso.
- [Validação](VALIDACAO.md): verificações efetivamente executadas.
- [Créditos](CREDITOS.md): fontes e ODbL.
- [Repositório](REPOSITORIO.md): desenvolvimento, testes e publicação.
- [Requisitos originais](REQUISITOS_ORIGINAIS.md): especificação inicial fornecida pelo autor.

`MapData` inclui os dois snapshots OSM, GeoJSON, proveniência, grafos derivados, métodos e licença. **© OpenStreetMap contributors — ODbL 1.0.**
