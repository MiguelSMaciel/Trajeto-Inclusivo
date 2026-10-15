# Arquitetura

Todos os scripts do produto usam o namespace `TrajetoInclusivo`. Há assemblies separados para execução, ferramentas do Editor e testes. A cena é um asset efetivamente salvo, com meshes e materiais persistidos; não depende de um tutorial manual para se tornar funcional.

| Módulo | Responsabilidade |
| --- | --- |
| AccessibilityProfile | Asset com capacidades individuais e pesos da simulação |
| AccessibilitySegment | Aresta, endpoints, largura, piso, inclinações, completude e proveniência |
| AccessibilityEvaluator | Avalia sentido da travessia, impedimentos e custo |
| AccessibilityObstacle | Condição física sintética associada explicitamente a um segmento |
| PedestrianGraph | Listas editáveis e índice de adjacência |
| RouteCalculator | Dijkstra e diagnóstico da ausência de rota |
| RouteAnalysis / RouteComparison | Métricas direcionais, gargalos, problemas por trecho e justificativa comparativa |
| ScenarioDefinition / ScenarioManager | Assets de condições de teste e aplicação/restauração |
| MapGenerator | Malhas de pavimento, meio-fio, colliders e volumes esquemáticos |
| ObstacleFootprints | Intervalos de ocupação e gargalo de largura contínua |
| SidewalkSurfaceBuilder | Pavimento subdividido, fundo/parede de buracos e degraus ajustados |
| AccessibilityRamp | Cotas, largura e inclinações de módulo de rampa |
| ElevationDataset / ElevationDataImporter | Grade local regular, validação e interpolação bilinear |
| ElevationSurfaceLayer | Deformação reversível do contexto e plano de referência |
| RuntimeWorldEditor / WorldState | Comandos, histórico limitado e persistência validada |
| SimulationController | Coordena eventos, agrupa alterações e recalcula |
| RouteVisualizer | Linhas reutilizadas por identificador; avaliação da rede e das duas rotas |
| SimulationUI | Interface IMGUI, seleção por raycast, propriedades e comparação |
| SimulationLayout | Escala, painéis e viewport a partir das dimensões da janela |
| LandmarkLabelLayout / SimulationUI.Landmarks | Rótulos de marcos na tela, foco de câmera e enquadramento após troca de trajeto |
| CameraController / RouteInspection | Navegação e acompanhamento cinemático |
| GeoDataImporter | GeoJSON, tags escalares e conversão em double antes da origem local |
| GeographicNetworkBuilder | Calçadas aproximadas, ligações hipotéticas e referências de origem OSM |
| GeographicMeshBuilder | Ribbons viários, triangulação côncava e extrusão de footprints |
| GeographicDatasetContext | Separa modo não avaliado de hipóteses explícitas de teste |
| GeographicSceneBuilder | Gera a cena da Etapa 2 a partir do snapshot local |
| TestbedBuilder | Gera cena, três perfis, cinco cenários, materiais, meshes e prefabs |
| DesktopBuild / WindowsBuild / LinuxBuild | Build Mono para Windows/Linux, seleção das três cenas e distribuição dos dados e documentos |

## Plataformas e distribuição

O código de execução compartilha as mesmas assemblies C# e URP nas duas plataformas; não depende de DLL nativa exclusiva do Windows. `DesktopBuild` concentra a preparação e recebe o `BuildTarget` dos menus Windows/Linux. O Linux usa `StandaloneLinux64`, Mono e OpenGL Core como primeira API gráfica, com Vulkan como alternativa.

As saídas em `Builds` incluem os snapshots e grafos em `MapData`, os guias e evidências em `Documentacao` e os avisos de licença. `Tools/Package-Linux.ps1` gera tar.gz com permissões Unix e checksum, excluindo o backup interno de símbolos do Unity. O workflow `linux-smoke.yml` testa o pacote publicado em Ubuntu com display virtual e renderização Mesa. Os fontes e arquivos `.meta` são versionados; caches, executáveis e saídas de teste locais são ignorados pelo Git.

## Custos

Distância = comprimento tridimensional entre os nós. Inclinação longitudinal = `100 × ΔY / distância horizontal XZ`. Inverter o sentido troca o sinal; limites e penalidades de subida e descida são independentes.

O custo de cada aresta passável é:

```
comprimento × (1
  + subida_em_% × peso_subida
  + descida_em_% × peso_descida
  + transversal_em_% × peso_transversal
  + irregularidade × peso_piso
  + déficit_relativo_de_largura × peso_largura
  + quantidade_de_obstáculos × peso_obstáculo)
```

O déficit de largura usa a faixa livre de referência, e é zero acima dela. Um limite individual excedido elimina a aresta: não existe uma penalidade finita capaz de tornar um impedimento aceitável. Pesos negativos são limitados a zero durante a avaliação para preservar as premissas de Dijkstra. Não se trata de energia, segundos, fadiga fisiológica ou nota de conformidade.

Os padrões foram escolhidos para demonstrar comportamentos de roteamento. O corte de dificuldade em 65% da tolerância direcional, a irregularidade de 0,20 e os pesos também são hipóteses de modelagem, sem origem normativa. Esses critérios de apresentação podem ser substituídos por uma política de classificação validada na evolução do avaliador.

## Dados ausentes e proveniência

`hasAssessmentData` distingue avaliação completa de ausência de avaliação. `verification` distingue simulado, importado não verificado e verificado em campo. São eixos separados: um trecho inteiramente fictício pode ter todos os parâmetros preenchidos, enquanto uma via real importada pode continuar sem qualquer avaliação de acessibilidade.

A rota acessível exclui trechos não avaliados. Para diagnosticar falha, o calculador testa primeiro conectividade da topologia e depois a hipótese de liberar apenas os dados desconhecidos. Ele distingue rede desconectada, dependência de dados insuficientes e impedimentos. A rota convencional ignora a capacidade individual e a falta de avaliação; preserva interdições físicas da rede.

Na rede, a cor do trecho usa o pior sentido. Nas linhas de rota, a cor usa o sentido efetivamente percorrido. Isso permite uma descida tolerada mesmo que a subida no sentido oposto esteja impedida.

## Geometria e atualização

O pavimento é um prisma com quatro vértices superiores derivados dos endpoints e da inclinação transversal. A malha e o MeshCollider usam a mesma superfície. Meios-fios são prismas contíguos e acompanham a elevação. A cota superior da calçada plana é 0,18 m acima do plano viário sintético.

Os obstáculos se ligam explicitamente ao segmento. `ObstacleFootprints` corta o eixo longitudinal nos limites das áreas ocupadas e une os intervalos laterais ativos em cada faixa. Usa o maior intervalo livre contínuo, limitado pela largura base, e toma o gargalo entre faixas. O formato legado de largura restante vira uma ocupação na borda direita; novos obstáculos usam largura ocupada e deslocamento lateral. Não há análise do volume varrido nem garantia de continuidade de manobra entre gargalos.

Buracos recortam a malha do trecho em células, com fundo deprimido e faces verticais onde a profundidade muda. Buracos sobrepostos usam o máximo da profundidade. Não há tampa plana sobre o fundo, e o collider usa a mesma malha. Ondulações subdividem a superfície e entram na irregularidade por uma conversão de simulação. Degraus usam um prisma ajustado aos cantos da superfície, com altura vertical constante no plano de base. O marcador é cinemático e percorre os centros dos trechos; não calcula desvio lateral fino ou contato de rodas.

Não há busca ou reconstrução de malha a cada frame. Uma mudança relevante marca o controlador como pendente. O próximo LateUpdate recalcula duas rotas e atualiza a visualização. A Etapa 2 usa Dijkstra com heap binário, O((V+E) log V), mantendo desempate pela ordem dos nós. Entradas antigas do heap são ignoradas quando uma distância melhor já foi encontrada.

Pavimentos têm cache de endpoints, largura, inclinação transversal, espessura, ondulações e buracos: mudanças apenas de perfil/custo não recriam suas malhas. A rede colorida usa uma malha combinada com cores por vértice; as duas rotas usam linhas reutilizadas. Eixos e edificações OSM são combinados por camada. Conexões sintéticas do mapa aparecem como guias com hitboxes de seleção, sem gerar um meio-fio para cada ligação hipotética. Não há NavMesh nem dependências pagas.

`RuntimeWorldEditor` agrupa as edições em comandos com snapshots JSON antes da mudança. Há até 12 passos de desfazer/refazer. A persistência é explícita, via salvar/carregar em `persistentDataPath`; validação de formato, fingerprint do grafo, IDs e números precede a restauração. A grade ativa e as cotas-base também são persistidas. O projeto e os assets não são modificados por uma sessão do player. Detalhes e limites em `EDICAO_E_RELEVO.md`.

`ElevationDataImporter` recebe CSV local regular, sem reprojeção implícita. `ElevationDataset` interpola dentro da cobertura, sem extrapolação. Aplicar elevação muda a base dos nós e deforma os vértices do contexto a partir das malhas originais, permitindo desfazer. Uma superfície auxiliar fica 0,40 m abaixo da cota de circulação; não é um DEM medido. As alterações de cenário são offsets sobre a base corrente. Proveniência vertical permanece simulada ou importada não verificada; importar cotas não completa os demais atributos de acessibilidade.

Na Etapa 4, `RouteComparison` é recriada junto das rotas, com avaliações por arco no sentido percorrido. O repaint do painel usa essa análise pronta. A aba dos três perfis calcula as hipóteses adicionais uma vez e invalida seu cache quando a comparação corrente muda. A interface separa números indisponíveis de valores zero e distingue problemas por categoria. Campos de texto identificados suspendem atalhos de teclado da câmera; seleção de endpoints só aceita colliders de nós pertencentes ao grafo.
