# Validação do projeto

Ambiente: Windows, Unity 6000.6.4f1, URP 17.6.0. Projeto em `E:/Jogos/Unity/Trajeto Inclusivo`.

## Versão 0.3.1 — janela e viewport — 28/10/2026

**78 testes aprovados, zero falhas:** 62 EditMode e 16 PlayMode. A suíte de layout agora cobre oito proporções e tamanhos, incluindo 800×600, 1600×1000 e 2560×1080. Confere que o viewport fica abaixo da barra e acima dos créditos, com a conversão correta entre as origens de coordenadas da câmera e da interface. O teste integrado de navegação verifica que os rótulos apontam para a projeção dos nós no viewport recortado. Após ajustar o bloqueio de cliques nas margens, esse teste foi executado novamente e aprovado.

**Windows 0.3.1 aprovado:** 60/60 combinações da matriz, inspeção nas três cenas, seis fatores de reprodução e zero erros de execução. A rotina alterou a janela para 1600×1000, 1280×720, 1920×1080 e 1024×768; em cada tamanho confirmou viewport alinhado à área central, rótulos projetados corretamente, margem superior preenchida e estado da experiência preservado. As capturas foram conferidas visualmente. A leitura dos estilos da janela pelo Win32 confirmou `WS_THICKFRAME` e `WS_MAXIMIZEBOX`, permitindo redimensionamento pelas bordas e botão de maximizar. Não foi simulado um clique físico nesse botão.

**Linux 0.3.1 aprovado:** o [workflow 40745406737](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40745406737) verificou o pacote publicado e o manifesto de runtime em Ubuntu 22.04.5 com Xvfb/Mesa. Passou em 60/60 combinações e nos quatro tamanhos reais de janela, com viewport, rótulos, margem superior e estado da experiência corretos. A rotina registrou `Runtime errors: 0`; capturas em 1920×1080 e 1024×768 foram conferidas. Esse ambiente não usa um gerenciador de janelas de desktop, portanto não verifica seu botão nativo de maximizar.

XMLs, relatórios, checksums, ambiente e capturas estão em [Resultados/Janela-0.3.1](Resultados/Janela-0.3.1). Os pacotes permanecem idênticos aos testados; as evidências Linux foram acrescentadas ao repositório após a execução do workflow.

## Versão 0.3.0 — inspeção de nós — 28/10/2026

**75 testes aprovados, zero falhas:** 59 EditMode e 16 PlayMode. Evidências em [Resultados/Nos-0.3.0](Resultados/Nos-0.3.0). Os três testes novos verificam seleção de nó por raycast sem alterar A/B, rota ou JSON da experiência, prioridade da escolha de A/B, atualização e remoção do destaque, passagem ao editor de um trecho, uso do nó como origem/destino, estado inicial não avaliado preservado e recusa de nós fora da rede.

A localização de alterações é exercitada nas três cenas: o cenário de rampa abre o nó elevado e o de degrau abre o trecho com obstáculo. Os mapas geográficos agora criam uma esfera clicável para o ponto GeoRamp, que antes só existia na estrutura da rede. Não houve regeneração das cenas ou mudança no grafo de roteamento.

**Windows 0.3.0 aprovado:** a rotina do executável passou em 60/60 combinações e confirmou inspeção de nós, preservação da experiência ao consultar, localização de alterações, uso como A/B e fechamento nas três cenas. Registrou `Runtime errors: 0` e `TRAJETO_STAGE4_OK`. A interface foi conferida em 1600×1000 e 1024×768. Relatórios e capturas em `Resultados/Nos-0.3.0/Windows`. A rotina aciona os mesmos métodos públicos e raycasts da interface; não simula cliques físicos do mouse na janela.

**Linux 0.3.0 aprovado:** o [workflow 40740969228](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40740969228) verificou o checksum do pacote publicado e o manifesto de runtime, passando em 60/60 combinações no Ubuntu 22.04.5 com Xvfb/Mesa. Confirmou a inspeção de nós nas três cenas, preservação da experiência ao consultar, localização de alterações, uso como A/B, fechamento e os seis fatores de reprodução. A rotina registrou `Runtime errors: 0`; as capturas da inspeção de GeoRamp e FAESA foram conferidas. Relatórios, ambiente, checksum e capturas em `Resultados/Nos-0.3.0/Linux`.

Os pacotes permanecem idênticos aos verificados; os registros Linux foram acrescentados ao repositório após a execução sobre o pacote publicado. A validação continua computacional, sem avaliação de campo ou ensaio de usabilidade com participantes.

## Versão 0.2.0 — velocidade de reprodução — 28/10/2026

**72 testes aprovados, zero falhas:** 59 EditMode e 13 PlayMode. Os XMLs estão em [Resultados/Velocidade-0.2.0](Resultados/Velocidade-0.2.0). Três novos testes integrados conferem os fatores 0,5×/1×/1,5×/2×/4×/8× durante o movimento, posição contínua ao mudar a velocidade, rota calculada preservada, manutenção ao parar/reiniciar/recalcular e chegada exata ao destino em alta velocidade. O tempo global permanece inalterado.

A rotina `-stage4-smoke` dos executáveis também verifica os seis fatores e captura a barra superior com 8× selecionado em `Playback-8x.png`, além da matriz existente de 60 casos. Os registros novos ficam em `Resultados/Velocidade-0.2.0`; o histórico anterior permanece abaixo.

**Windows 0.2.0 aprovado:** executável extraído do ZIP passou em 60/60 combinações, com os seis multiplicadores e posição contínua confirmados, rota e tempo global preservados e `Runtime errors: 0`. Capturas em 1600×1000 (8×) e 1024×768 foram conferidas, sem sobreposição dos novos botões. Evidências em `Resultados/Velocidade-0.2.0/Windows`.

**Linux 0.2.0 aprovado:** o [workflow 40723345470](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40723345470) verificou o pacote publicado e seu manifesto de runtime e passou em 60/60 combinações no Ubuntu 22.04.5 com Xvfb/Mesa. Os seis fatores de reprodução, posição contínua, rota e tempo global foram novamente confirmados; a rotina registrou `Runtime errors: 0`. A captura em 8× foi conferida. Relatórios, ambiente, checksum do pacote e captura estão em `Resultados/Velocidade-0.2.0/Linux`. Os pacotes publicados permanecem idênticos aos testados; estas evidências Linux foram acrescentadas ao repositório após a execução.

## Port Linux

Build nativo Linux x86_64/Mono concluído em 15/10/2026 com o módulo Linux Build Support instalado oficialmente pelo Unity Hub. `linux-final-build.log` registrou `TRAJETO_LINUX_BUILD_OK` e encerramento com código 0. O pacote tar.gz preserva permissões 0755 do player e dos scripts; o cabeçalho ELF confirma arquitetura x86_64.

**Execução nativa aprovada: 60/60 combinações**, nas três cenas, em **Ubuntu 22.04.5 LTS x86_64**, kernel 6.8, Unity 6000.6.4f1/Mono, X11/Xvfb e Mesa llvmpipe OpenGL 4.5. O [workflow 38794056999](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/38794056999) baixou o pacote da versão, validou SHA-256 e terminou com sucesso. O relatório registra seleção de endpoints/A=B nas três cenas, estado inicial não avaliado nos mapas geográficos e aproximação da FAESA preservando os endpoints. São 60 linhas `PASS`, zero `FAIL`, `Runtime errors: 0` na rotina do aplicativo e `TRAJETO_STAGE4_OK`.

Os [relatórios e 13 capturas](Resultados/Linux) estão preservados no repositório e na distribuição. As imagens de comparação e aproximação da FAESA foram conferidas visualmente. O teste não incluiu cliques manuais de um usuário; usa as mesmas ações e raycasts da interface. O log nativo contém avisos de ausência de dispositivo de áudio no runner e fallback FMOD sem som; não houve validação de áudio. A renderização por software não comprova desempenho em GPU física. Vulkan, Wayland, Ubuntu 24.04 e outras distribuições não foram executados nesta validação.

`Resultados/Linux/tested-package-sha256.txt` identifica o pacote baixado nessa execução. Após o teste, foram acrescentados documentos e evidências à distribuição; os arquivos do player permanecem idênticos e estão identificados em `Resultados/Linux/player-files-sha256.txt`. `SHA256SUMS.txt`, ao lado do tar.gz na versão publicada, verifica o pacote final completo. A compilação final das ferramentas de documentação encerrou com código 0 em `linux-final-compile.log`.

As evidências anteriores do Windows (59 EditMode + 10 PlayMode e matriz de 60 casos do player) estão preservadas em [Resultados/Windows](Resultados/Windows). Não representam execução Linux.

## Verificações concluídas

**Publicação Windows — 19/10/2026:** o programa extraído do ZIP de distribuição passou novamente em **60/60 casos**, nas três cenas, com `Runtime errors: 0` e `TRAJETO_STAGE4_OK`. Os registros estão em `Resultados/Windows/publicacao-2026-10-19-player.txt` e `publicacao-2026-10-19-player.csv`. O ZIP inclui executável, UnityPlayer, Mono, DLLs, dados OSM/FAESA e documentação; os backups de símbolos marcados pelo Unity como não distribuíveis foram excluídos. Depois do teste, somente os documentos do pacote foram atualizados para incluir estes registros. O checksum de distribuição Windows é publicado em `SHA256SUMS-Windows.txt`, separadamente do checksum Linux.

**Navegação no mapa ampliado:** `TestResults/navigation-editmode.xml` registra **59 testes aprovados**, e `navigation-playmode.xml` registra **10 testes integrados aprovados**, zero falhas: **69 testes** no total. Os novos casos conferem separação e contenção dos rótulos, inclusive quando os pontos projetados coincidem ou ficam além da tela; aproximação dos três marcos preservando endpoints, a mesma rota calculada e o estado não avaliado; cancelamento de seleção pendente; enquadramento após trocar o trajeto; e proteção contra seleção da geometria por trás dos rótulos e do rodapé. Os resultados anteriores abaixo são mantidos como histórico.

O build foi concluído em `navigation-build.log`. O executável verificou novamente **60/60 combinações**, aproximação da FAESA preservando A/B e zero erros de execução (`TestResults/Navigation/Stage4-Player.txt`, `Stage4-Player-Matrix.csv`, `navigation-player.log`). As capturas `Navigation/Navigation-FAESA-Closeup.png` e `Navigation/Stage4-Comparacao-AvenidaVitoria_FAESA.png` foram conferidas visualmente, incluindo rótulos, barra de aproximação e ocultação dos textos geográficos grandes na vista próxima. A rotina automatizada usa as ações da interface; não houve ensaio manual de usabilidade. **© OpenStreetMap contributors — ODbL 1.0.**

**Expansão FAESA:** `TestResults/faesa-editmode.xml` registra **54 testes aprovados** e `faesa-playmode.xml` registra **9 testes integrados aprovados**, zero falhas: **63 testes** no total. A nova cena foi gerada com 750 eixos/caminhos, 1.212 edifícios, 1.608 nós, 2.522 trechos e zero polígonos rejeitados (`FaesaSetup.txt`). O percurso inicial Praça–FAESA mede cerca de 654,39 m no modelo, sem declarar itinerário real verificado.

`FAESA-Matrix.csv` guarda 30 combinações da cena ampliada: Praça/Ifes como origem × 5 cenários × 3 perfis. Todas encontram caminho convencional e acessível quando as hipóteses são explicitamente ativadas; os arcos acessíveis são conferidos como passáveis. Os testes verificam estado inicial não avaliado, IDs únicos, ausência de acesso artificial com comprimento zero na referência projetada, conexão à Avenida Vitória, botões de referência, enquadramento e retorno ao estado desconhecido. Também conferem checksum, endereço OSM 2220, fonte institucional e preservação da origem local. As suítes anteriores continuam aprovadas.

**Build da expansão concluído** (`faesa-build.log`, `TRAJETO_BUILD_OK`). O executável confirmou **60/60 combinações**, zero erros de execução e seleção por raycast nas três cenas (`TestResults/FAESA/Stage4-Player-Matrix.csv`, `Stage4-Player.txt`, `faesa-player.log`, marcador `TRAJETO_STAGE4_OK`). São os 30 casos das cenas anteriores mais 30 casos da expansão. Capturas completas com IMGUI foram conferidas em `TestResults/FAESA/Stage4-Comparacao-AvenidaVitoria_FAESA.png` e `Stage4-NaoAvaliado-AvenidaVitoria_FAESA.png`, mostrando o recorte ampliado, 654,4 m no caminho convencional e 665,4 m no desvio do degrau. O estado inicial exibe apenas a rota convencional e mantém a acessível sem confirmação. **© OpenStreetMap contributors — ODbL 1.0.** A validação é computacional; não houve inspeção de campo ou ensaio manual de usabilidade.

**Etapa 4:** `TestResults/stage4-editmode.xml` registra **52 testes aprovados**, zero falhas, e `TestResults/stage4-playmode.xml` registra **8 testes integrados aprovados**, zero falhas. São **60 testes**, incluindo comparação direcional, justificativas, layout em cinco proporções de tela, seleção de endpoints por raycast, cancelamento, A=B, cache de perfis e foco por trecho.

A matriz de 2 cenas × 5 cenários × 3 perfis passou integralmente. Os CSVs `Stage4-AvenidaVitoria_Testbed-Matrix.csv` e `Stage4-AvenidaVitoria_Geografico-Matrix.csv` guardam as 30 combinações. As cadeiras manual e motorizada evitam degrau, rampa e buraco dos presets; o pedestre consegue usar o caminho central nesses três casos. Obras provocam desvio das duas buscas para todos os perfis. Na rede geográfica, o cenário inclinado tem dois trechos impedidos para a cadeira manual e um para a motorizada, devido às tolerâncias diferentes de descida.

**Build da Etapa 4 concluído** em `TestResults/stage4-build.log`. O executável confirmou **30/30 combinações aprovadas**, seleção de endpoints nas duas cenas e zero erros de execução (`TestResults/Stage4-Player-Matrix.csv`, `Stage4-Player.txt`, `stage4-player.log`, marcador `TRAJETO_STAGE4_OK`). A rotina descarta capturas pretas da janela oculta. A recriação do framebuffer permitiu capturar e conferir IMGUI: estado não avaliado, comparação com desvio, três perfis, inspeção e layout em 1024×768, nas imagens `Stage4-*.png`. A conferência identificou e corrigiu vestígios visuais entre abas e baixo contraste dos botões de problemas. Não houve automação de cliques de mouse/teclado na janela nativa; os fluxos foram exercitados pelas mesmas ações públicas e raycasts usados pela interface.

`Docs/INTERFACE_E_VALIDACAO.md` relaciona os sete critérios do pedido às verificações e explica os fluxos do painel final. Os registros das etapas anteriores abaixo preservam seu histórico de validação.

**Etapa 3:** `TestResults/stage3-editmode.xml` registra **42 testes aprovados**, zero falhas; `TestResults/stage3-playmode.xml` registra **5 testes integrados aprovados**, zero falhas. Os relatórios anteriores abaixo preservam a validação das etapas precedentes.

Os novos testes conferem largura de passagem contínua entre obstáculos centrais, laterais sobrepostos e sucessivos; contorno de degrau parcial conforme perfil; altura vertical do degrau sobre plano inclinado; collider atingindo o fundo do buraco; sobreposição de profundidades; ondulação física; interpolação bilinear com origem vertical, limites da cobertura e rejeição de CSV inválido. Os testes em execução exercitam edição que provoca desvio de 148 m, desfazer/refazer, salvar/carregar, recusa de arquivo incompatível sem modificar o mundo, rampa de 12%, grade persistindo entre cenários e restauração de cotas-base. Aplicar elevação mantém a rede geográfica não avaliada quando esse era o estado anterior.

**Build da Etapa 3 concluído** (`TestResults/stage3-build.log`, `TRAJETO_BUILD_OK`). **Validação no executável Windows aprovada** (`TestResults/stage3-player.log`, `TRAJETO_PLAYER_SMOKE_OK`). Além dos cinco cenários geográficos, o player conferiu fundo do buraco e largura contínua, desfazer/refazer, equivalência do JSON após salvar/carregar, rampa editada e aplicação de grade artificial. Os resultados estão em `TestResults/GeographicSmoke.txt`; a experiência usada está em `TestResults/Stage3-Player.experiment.json`.

As capturas `Stage3-Obstaculos.png` e `Stage3-Relevo.png` foram renderizadas pelo player e conferidas visualmente: mostram o buraco na calçada inclinada, poste/árvore/banco esquemáticos e o contexto OSM com a grade de demonstração aplicada. Não incluem a interface IMGUI nem constituem observações reais. **© OpenStreetMap contributors — ODbL 1.0.** Não houve teste manual de cliques/teclado na janela visível; os testes e o player exerceram os comandos e o ciclo de renderização.

- Geração e gravação da cena `AvenidaVitoria_Testbed`: concluída pelo Editor em batch (`TRAJETO_SETUP_OK`).
- **EditMode: 29 testes aprovados, zero falhas.** Relatório NUnit em `TestResults/EditMode.xml`, atualizado após integrar o mapa OSM e o Dijkstra com heap.
- **PlayMode: 2 testes integrados aprovados, zero falhas.** Relatório em `TestResults/PlayMode.xml`; cobre as duas cenas.
- **Build Windows: concluído.** Executável em `Builds/Windows/TrajetoInclusivo.exe`.
- **Player Windows do testbed original: cinco cenários aprovados na Etapa 1.** `TestResults/StandaloneSmoke.txt` registra 100,018 m na rota central suave, 100,287 m na rota central com 12% e 148,000 m nos desvios esperados.

Na Etapa 2, a geração importou 496 eixos/caminhos/escadarias e 787 edifícios, sem rejeição de polígonos. A rede experimental tem 890 nós e 1.512 trechos. O relatório em `TestResults/GeographicSetup.txt` registra a conexão geométrica inicial, de aproximadamente 186,52 m, sem afirmar que seja um trajeto pedestre real verificado.

O teste integrado geográfico confirma que a rede começa não avaliada, sem rota acessível confirmada; a ativação explícita de hipóteses permite os cenários de teste, e as barreiras provocam desvios. Também verifica que mudar o perfil reutiliza as malhas dos pavimentos. Os testes GIS conferem checksum do XML, nomes/tags preservados, falta de elevação, normais e área da triangulação côncava, não conexão de eixos apenas por cruzamento visual e separação por nível.

**Player Windows geográfico: cinco cenários aprovados**, incluindo verificação do estado inicial não avaliado. `TestResults/GeographicSmoke.txt` registra 186,519 m no percurso base, 187,145 m na variante inclinada e 352,693 m no desvio acessível; em obras, ambas as buscas usam 352,693 m. Essas distâncias pertencem ao grafo experimental, com conexões e atributos simulados. `TestResults/geographic-player.log` registra a execução. O player foi renderizado via URP para conferir eixos, footprints e percursos nas imagens `Geographic-*.png`.

A distribuição inclui `MapData/pedestrian_graph.json`, com 890 IDs de nós únicos, 1.512 arestas inicialmente não avaliadas e proveniência por trecho. Inclui também os parâmetros de simulação e os métodos necessários para reproduzir a derivação. A atribuição dessas prévias e da base é **© OpenStreetMap contributors — ODbL 1.0**, conforme `Docs/CREDITOS.md`.

As prévias `TestResults/Testbed-Perspectiva.png` e `TestResults/Testbed-Superior.png` são renderizações geométricas offscreen do player, conferidas visualmente. Não incluem o painel IMGUI. A interação manual com a janela visível não foi exercitada por automação de desktop nesta sessão.

O teste integrado carrega a cena real do projeto e exercita os cinco cenários com cadeira manual. Confere caminho direto no cenário livre, desvio de 148 m diante de degrau, rampa de 12% e buraco, desvio de ambas as buscas diante das obras, restauração da rota após remoção do degrau, capacidade diferente do pedestre e movimento do marcador. Confere também a geometria/collider da rampa elevada. `LogAssert.NoUnexpectedReceived` verifica logs inesperados durante essa execução.

Os testes de EditMode também cobrem Dijkstra e métricas geométricas, limites individuais, pesos, trechos desconhecidos, topologia sem conexão, interdições, sentido único, perfis direcionais e importação de coordenadas. Não constituem uma validação geodésica contra pontos de controle externos ou validação de normas.

## Reproduzir por linha de comando

Exemplos PowerShell, executados a partir da pasta do projeto:

```powershell
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe'
$testProject = (Get-Location).Path
& $unityEditor -batchmode -nographics -projectPath $testProject -runTests -testPlatform EditMode -testResults "$testProject/TestResults/EditMode.xml" -logFile "$testProject/TestResults/editmode.log"
& $unityEditor -batchmode -projectPath $testProject -runTests -testPlatform PlayMode -testResults "$testProject/TestResults/PlayMode.xml" -logFile "$testProject/TestResults/playmode.log"
& $unityEditor -batchmode -projectPath $testProject -executeMethod TrajetoInclusivo.Editor.WindowsBuild.Build -quit -logFile "$testProject/TestResults/build.log"
```

Não execute instâncias concorrentes do Editor sobre este mesmo projeto. O processo de testes encerra o Editor ao terminar; evite `-quit` na chamada `-runTests`.

O player tem uma entrada opcional de QA, ativada exclusivamente pelo argumento `-testbed-smoke`. Ela percorre os cenários, registra distâncias, salva capturas e encerra. No uso normal, esse componente não é criado.

```powershell
& './Builds/Windows/TrajetoInclusivo.exe' -testbed-smoke -testbed-results "$testProject/TestResults" -logFile "$testProject/TestResults/player.log"
```

## Alcance

As verificações comprovam o comportamento dos exemplos sintéticos conhecidos. Não houve levantamento da Avenida Vitória, avaliação com usuários reais, ensaio de cadeira física ou comprovação de conformidade normativa. Cotas, gargalos de passagem e capacidades dos perfis continuam classificados como hipóteses artificiais.
