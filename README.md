# Trajeto Inclusivo — Unity / Avenida Vitória

**Manual de uso e apresentação:** [leia o manual completo](Docs/MANUAL_DE_USO.md), com telas explicadas, exercícios guiados e roteiro de demonstração de 12 minutos. Baixe o [PDF ilustrado](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/releases/download/v0.2.0/Manual_Trajeto_Inclusivo_0.2.0.pdf) ou o [Word editável](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/releases/download/v0.2.0/Manual_Trajeto_Inclusivo_0.2.0.docx). O manual tem 25 páginas; as páginas 20 a 22 ajudam a ensaiar a apresentação.

**Corrigido na 0.3.1:** a janela pode ser redimensionada e maximizada. O mapa ocupa a área central abaixo da barra, sem a faixa recortada no topo, e os rótulos acompanham o viewport ao mudar o tamanho. Consulte [janela e enquadramento](Docs/JANELA_E_LAYOUT.md).

Desde a 0.3.0, clique numa **bolinha azul** para abrir **Conhecer o nó**, com nome, identificador, papel como A/B, cota, fonte e trechos conectados. Um anel amarelo marca o nó selecionado. **Localizar alteração do cenário** aproxima o nó elevado ou o trecho com obstáculo. Veja o [guia ilustrado de inspeção de nós](Docs/INSPECAO_DE_NOS.md), complemento do manual base da versão 0.2.0.

A barra superior permite mudar a velocidade do acompanhamento para **0,5×, 1×, 1,5×, 2×, 4× e 8×**, sem reiniciar o percurso.

**Windows x86_64:** baixe `TrajetoInclusivo-Windows-x86_64.zip` na [página de versões](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/releases/tag/v0.3.1), extraia toda a pasta e execute `TrajetoInclusivo.exe`. O ZIP inclui as três cenas, os dados e a documentação; mantenha as DLLs e pastas junto do executável. O checksum separado está em `SHA256SUMS-Windows.txt`.

**Linux x86_64:** há um port nativo com as três cenas e toda a documentação. Consulte [LINUX.md](Docs/LINUX.md) para executar, compilar e validar. Os pacotes ficam na [página de versões](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/releases); o repositório contém os fontes, assets e documentação, sem os caches do Unity.

**Validação Linux:** 60/60 casos aprovados em Ubuntu 22.04.5, usando Xvfb/Mesa. Relatórios, capturas e limites em [VALIDACAO.md](Docs/VALIDACAO.md). Instruções para desenvolvimento e publicação em [REPOSITORIO.md](Docs/REPOSITORIO.md).

O projeto inclui as quatro etapas: testbed sintético, integração cartográfica, componentes físicos/editáveis e interface comparativa com validação automatizada. O mapa de Jucutuquara, Vitória–ES, usa eixos e polígonos do **OpenStreetMap extraídos em 15/10/2026**, preserva nomes e referências de origem, e funciona localmente.

**Expansão até a FAESA:** a nova cena `AvenidaVitoria_FAESA` inclui o corredor Praça–Ifes–FAESA, com botões **Praça → FAESA** e **Ifes → FAESA**. O executável abre esse mapa ampliado. O destino é uma referência aproximada na calçada da Avenida Vitória junto ao endereço 2220; não representa uma entrada verificada do campus. Fonte, método e limites em `Docs/EXPANSAO_FAESA.md`.

Use os botões **Aproximar** da barra central ou os rótulos clicáveis no mapa para localizar a praça, o Ifes e a referência da FAESA. Aproximar preserva A/B e os dados da experiência; trocar o trajeto pelos atalhos enquadra a nova rota automaticamente.

**Calçadas deslocadas dos eixos, conexões, larguras inferidas, alturas aproximadas, cotas e obstáculos continuam hipóteses de teste.** Não há avaliação de campo, DEM ou certificação de acessibilidade. A geometria mapeada não transforma uma rota em um itinerário pedestre verificado.

## Executar

1. No Unity Hub, escolha **Add / Add project from disk** e selecione esta pasta.
2. Use **Unity 6000.6.4f1**, URP 17.6.0 e o módulo da plataforma de destino (Windows ou Linux Build Support/Mono). Outra versão Unity 6 pode exigir compatibilização de pacotes.
3. Abra `Assets/TrajetoInclusivo/Scenes/AvenidaVitoria_FAESA.unity` para o mapa ampliado, ou `AvenidaVitoria_Geografico.unity` para o recorte original.
4. Pressione **Play**. A cena ampliada inicia na Praça Asdrúbal Soares, com destino na referência da FAESA; a original tem destino junto a um portão do Ifes mapeado no OSM. Os acessos e calçadas são aproximações.
5. Inicialmente, os trechos estão **não avaliados** e nenhuma rota acessível é confirmada. Ative **“Ativar hipóteses de teste (SIMULAÇÃO)”** para comparar cenários com parâmetros artificiais explícitos.

O executável `Builds/Windows/TrajetoInclusivo.exe` abre a cena ampliada até a FAESA. Os botões permitem alternar entre as três cenas. A pasta `MapData` contém os dados, métodos e créditos distribuídos sob ODbL; `MapData/FAESA` contém o snapshot ampliado e seu grafo.

A entrega Windows também inclui `Documentacao`, com instruções, fontes, limites e os resultados da Etapa 4 em `Documentacao/Resultados`. Mantenha toda a pasta `Builds/Windows` ao distribuir ou executar em outra máquina.

Para repetir o exemplo pequeno original, abra `Assets/TrajetoInclusivo/Scenes/AvenidaVitoria_Testbed.unity`. Ele inicia com cadeira manual e cenário de degrau.

Se a cena precisar ser recriada, use **Trajeto Inclusivo → Criar ou reconstruir testbed sintético**. Esse comando recria a cena e os assets de demonstração; salve suas alterações em cópias antes de reconstruir.

Para reconstruir a cena geográfica com o snapshot local, use **Trajeto Inclusivo → Gerar cena geográfica do recorte OSM local**. O comando não baixa dados. `Tools/Export-OsmGeoJson.ps1` reproduz o GeoJSON a partir do XML original. Ambos os geradores substituem suas respectivas cenas de demonstração.

Também é possível gerar um programa local com **Trajeto Inclusivo → Gerar executável Windows**. O resultado fica em `Builds/Windows/TrajetoInclusivo.exe`; mantenha as DLLs e a pasta `_Data` ao lado do executável. O programa e os testes não consultam serviços geográficos nem exigem internet. O primeiro carregamento do projeto pode precisar restaurar pacotes se não estiverem disponíveis na instalação/cache.

## Usar o protótipo

- **Condição artificial:** alterne os cinco cenários no painel esquerdo. Eles são assets editáveis, sem necessidade de mudar código.
- **Perfis:** pedestre, cadeira manual e cadeira motorizada. “Consultar limites dos perfis” abre a aba **Três perfis**, com capacidades, resultado de cada hipótese e botão para selecioná-la. Os parâmetros são hipóteses de teste.
- **Nós e A / B:** um clique comum numa esfera azul abre suas informações sem mudar a rota. No painel **Conhecer o nó**, **Usar como A/B** define o ponto como origem/destino. Também é possível clicar em “Escolher A” ou “Escolher B” e depois numa esfera. “Inverter” troca o sentido; “Cancelar seleção” ou Esc cancela. A=B produz um percurso de distância zero. Esc ou `×` fecha a inspeção.
- **Comparação:** a tabela do painel direito mostra as duas rotas lado a lado: distância, subida/descida acumuladas, inclinações direcionais e transversal, menor faixa livre, obstáculos e custo ponderado acessível. Exibe desvio em metros/porcentagem e justificativa por barreira, dados ausentes ou custo. Clique nos trechos problemáticos listados para abrir e focar sua geometria. A rota selecionada fica mais espessa.
- **Inspeção:** clique no pavimento ou em um obstáculo. Ajuste cotas/rampa, largura, inclinação transversal, irregularidade e ondulação física; clique em **Aplicar** para confirmar. Adicione ou remova poste, árvore, lixeira, mobiliário, objeto, degrau, buraco ou obras. Cada obstáculo tem área ocupada, posição lateral/longitudinal, altura/profundidade e contorno. Buracos escavam o pavimento e seu collider. Feche o painel no `×` para voltar à comparação.
- **Histórico e persistência:** a barra central permite desfazer/refazer até 12 comandos e salvar/carregar uma experiência JSON na pasta de dados do aplicativo. A mensagem mostra o caminho. Inclui obstáculos, geometria, grade, estado de avaliação, A/B e perfil.
- **Elevação:** no painel de um trecho, aplique a grade artificial de demonstração ou importe um CSV local regular `x,z,elevation`. Informe a mesma origem horizontal da cena, referência vertical e origem a subtrair. Dados importados permanecem não verificados; não habilitam avaliação automaticamente. Veja `Docs/EDICAO_E_RELEVO.md`.
- **Câmera:** WASD move; Q/E altera altura; botão direito orbita; roda aproxima/afasta; T alterna vista superior; Home enquadra a área. “Focar percurso disponível” aproxima o caminho calculado para inspecionar seus trechos. Durante a digitação em campos, os atalhos de teclado ficam suspensos; clique no mapa para voltar à navegação. Esc fecha o editor de trechos.
- **Acompanhar rota acessível:** anima uma cadeira esquemática cinematicamente. Na barra superior, **Velocidade** permite escolher **0,5×, 1×, 1,5×, 2×, 4× ou 8×**, inclusive durante o movimento, sem reiniciar o percurso. O padrão é 1×. Alterações de rota encerram a inspeção em andamento; a velocidade escolhida é preservada na cena. Não há dinâmica de rodas, balanço ou esforço biomecânico.

Verde significa acessível **pelas hipóteses do perfil**; amarelo indica dificuldade; vermelho indica impedimento; cinza indica dados insuficientes. A linha azul representa o caminho convencional, que pode atravessar um trecho vermelho para o perfil. Interdições físicas eliminam a passagem de ambas as buscas.

## Resultados de referência

**No testbed sintético**, os corredores planos de contorno medem **148 m** e o caminho central aproximadamente **100 m**. Esses números não descrevem o percurso real entre a praça e o Ifes.

| Cenário | Condição artificial | Resultado esperado com cadeira manual |
| --- | --- | --- |
| 1 | Sem obstáculos; rampa central de 3% | Ambas as buscas usam o caminho central |
| 2 | Degrau de 0,16 m | Convencional central; acessível contorna por 148 m |
| 3 | Subida e descida de 12%, com 2,40 m de elevação em 20 m horizontais | Convencional central; acessível contorna por 148 m |
| 4 | Buraco deixa 0,65 m livres | Convencional central; acessível contorna por 148 m |
| 5 | Obras interditam uma aresta central | Ambas as buscas contornam por 148 m |

Nos empates, a ordem dos nós serializados torna o resultado determinístico. É possível escolher outros pontos para explorar novas combinações.

Na cena geográfica, o percurso convencional inicial do snapshot é aproximadamente **186,52 m no modelo**, sobre calçadas e conexões aproximadas. Isso não é uma medição de um itinerário real. Os cinco cenários geográficos têm assets próprios em `Data/Geography/Scenarios`; o trecho artificial é escolhido entre os trechos do caminho convencional que possuem alternativa conectada.

## Editar no Inspector

Os perfis ficam em `Assets/TrajetoInclusivo/Data/Profiles`. Ajuste largura necessária, degrau máximo, limites distintos de subida/descida, inclinação transversal, irregularidade e pesos de custo. A faixa livre de referência começa em **1,20 m**; abaixo disso, um trecho pode ser difícil e continuar passável para um perfil mais estreito. Isso **não declara conformidade normativa**.

Os cenários ficam em `Assets/TrajetoInclusivo/Data/Scenarios`. Defina o nó elevado, seu deslocamento vertical e uma lista de obstáculos por identificador de trecho. Ao trocar de cenário, larguras e avaliações editadas durante a execução voltam à base capturada ao iniciar. A grade de cotas aplicada persiste como base; deslocamentos de cenário são somados a ela. O histórico permite recuperar a experiência anterior.

Os componentes `AccessibilitySegment` e `AccessibilityObstacle` expõem dados no Inspector. Alterações notificadas recalculam rotas no próximo `LateUpdate`, agrupando múltiplas mudanças no mesmo quadro. Se mover um nó diretamente pelo Transform no Editor em Play, use o menu de contexto do componente `SimulationController → Recalcular rotas e geometria` ou mude um parâmetro do trecho para notificar a geometria; os nós não são monitorados por varredura contínua. Os anéis verde e violeta acompanham a origem e o destino escolhidos; os identificadores dos nós permanecem fixos.

Há prefabs de calçada, rampa, obstáculo e cadeira em `Assets/TrajetoInclusivo/Prefabs`. Depois de colocar um módulo de calçada, atribua seus nós inicial/final, registre-o no `PedestrianGraph` e gere sua geometria por `MapGenerator.RefreshGeometry()`. Os prefabs não constituem uma rede automaticamente. O menu **Trajeto Inclusivo → Preparar recursos de obstáculos e relevo** cria os assets de exemplo ausentes.

## Testes

Abra **Window → General → Test Runner**. Execute as abas **EditMode** e **PlayMode**.

- EditMode: roteamento, métricas direcionais e comparação, importação, checksum do snapshot, triangulação, separação por ID/nível, gargalos entre obstáculos, degrau parcial, fundos físicos de buracos, ondulação, interpolação/rejeição de grades CSV e separação dos painéis em diferentes resoluções.
- PlayMode: carrega as três cenas, verifica a matriz de cinco cenários com três perfis, seleção de pontos por raycast, A=B, cache de comparações, foco por trecho, desvios, rampas, dados não avaliados, reutilização de malhas, edição física, histórico, persistência e permanência da grade entre cenários.

Os relatórios da validação local ficam em `TestResults`. Consulte `Docs/VALIDACAO.md` para os resultados efetivamente executados.

## Integração geográfica

`GeoDataImporter` lê GeoJSON local e converte WGS84 longitude/latitude ou SIRGAS 2000 / UTM 24S para metros locais: X leste, Z norte e Y altitude. A origem da cena geográfica vem do nó OSM `12511666338` no contorno do Ifes. O menu **Trajeto Inclusivo → Inspecionar GeoJSON local** valida outros `.json` com origem explicitamente informada.

`GeographicSceneBuilder` integra eixos e footprints ao cenário; `GeographicNetworkBuilder` constrói calçadas e conexões hipotéticas. Usa IDs OSM e níveis, não conecta vias apenas por cruzamento visual e mantém dados sem avaliação fora da busca acessível. `Data/Geography/AvenidaVitoria.asset` contém a referência e as dimensões assumidas. Veja `Docs/DADOS_GEOGRAFICOS.md` para fontes e limites.

## Limites desta entrega

Estão implementadas as Etapas 1 a 4. O testbed pequeno tem 13 nós e 18 arestas. A cena geográfica usa footprints e eixos importados, com atributos não levantados separados. Há escavação geométrica local de buracos, ondulação, rampas e importação de elevação; a grade incluída é artificial. Não foi incorporado um DEM real de Vitória. Não há simulação de tráfego, levantamento de campo ou certificação de acessibilidade. O cálculo de largura considera gargalos locais, sem simular manobras ou a continuidade de um desvio lateral fino. Veja `Docs/INTERFACE_E_VALIDACAO.md` para os fluxos da interface e a relação entre critérios de sucesso e testes.

Os limites dos perfis e os pesos não foram validados com usuários nem com a ABNT. Para uma etapa normativa, consulte a edição aplicável da NBR 9050 e normas complementares, registre a fonte e mantenha essa verificação separada da capacidade individual de deslocamento.

Créditos e fontes: `Docs/CREDITOS.md`. **© OpenStreetMap contributors — ODbL 1.0.** Atribuição visível na cena geográfica e dados distribuídos com o projeto e o executável.
