<!-- Especificação inicial fornecida pelo autor. O estado implementado, a expansão FAESA e o port Linux estão descritos no README e nos demais guias. -->

# Projeto: Simulador de Acessibilidade Urbana — Avenida Vitória, ES

## 1. Objetivo

Desenvolva um protótipo 3D interativo na Unity, utilizando C#, para simular e avaliar a acessibilidade de trajetos urbanos para pessoas que utilizam cadeiras de rodas.

O cenário deverá utilizar como referência uma região real da Avenida Vitória, em Vitória, Espírito Santo, Brasil.

O sistema deverá considerar distância, elevação, inclinação, qualidade do pavimento, buracos, degraus, rampas, obstáculos e largura disponível para circulação.

O objetivo central é permitir comparar uma rota convencional, baseada principalmente na distância, com uma rota otimizada para acessibilidade.

**Prioridade:** funcionamento, precisão geométrica, organização do código, desempenho e possibilidade de alterar os cenários.

Não desenvolver um jogo completo nem gastar tempo com modelos altamente detalhados.

## 2. Região geográfica

**Local:** Avenida Vitória, bairro Jucutuquara, Vitória–ES, Brasil.

**Referências reais:**
- Instituto Federal do Espírito Santo (Ifes), Campus Vitória — Avenida Vitória, 1729.
- Praça de Jucutuquara.
- Vias, quadras, edifícios e cruzamentos existentes no entorno.

Utilize inicialmente uma pequena área em torno do Ifes e da Praça de Jucutuquara, suficiente para conter múltiplas alternativas de deslocamento.

O ponto A poderá ser definido próximo à Praça de Jucutuquara, e o ponto B na região do Ifes. A posição definitiva de ambos deverá seguir os dados cartográficos importados.

### Fontes geográficas

Priorize:
1. OpenStreetMap — https://www.openstreetmap.org
2. GeoWeb Vitória — https://geoweb.vitoria.es.gov.br
3. Arquivos GeoJSON, GeoPackage, KML ou outros dados vetoriais exportados do QGIS.
4. Dados públicos de elevação, quando disponíveis e licenciados para uso.

Preserve os nomes reais de vias e pontos de referência.

Não invente coordenadas, cruzamentos ou dimensões exatas e apresente a origem dos dados geográficos utilizados.

Quando não houver geometrias confiáveis de calçadas, recrie-as aproximadamente como elementos sintéticos de teste, identificando claramente essa diferença.

**Importante:** obstáculos, buracos, degraus e condições de acessibilidade que não tenham sido verificados no mundo real deverão ser explicitamente classificados como dados simulados.

## 3. Configuração técnica

- Engine: Unity 6, projeto 3D com URP.
- Linguagem: C#.
- Escala: 1 unidade Unity = 1 metro.
- Plataforma inicial: Windows.
- Geometria modular e uso de prefabs reutilizáveis.
- Sem dependências pagas.
- Organização de scripts por responsabilidade.
- Suporte a dados editáveis no Inspector.
- Priorizar execução local, sem necessidade de internet durante os testes.

Utilize um sistema de coordenadas local a partir de um ponto geográfico de referência.

Os arquivos GIS poderão utilizar SIRGAS 2000 / UTM 24S (EPSG:31984). Converta as coordenadas geográficas para posições locais em metros, utilizando X para leste, Z para norte e Y para altitude. Evite perda de precisão por coordenadas globais muito grandes.

## 4. Geração do cenário 3D

Crie uma cena denominada `AvenidaVitoria_Testbed`.

Ela deverá conter:

- Ruas e cruzamentos.
- Calçadas dos dois lados das vias.
- Meio-fios.
- Travessias de pedestres.
- Rampas de acesso.
- Edificações simplificadas.
- Áreas de elevação variável.
- Superfícies com irregularidades.
- Obstáculos urbanos.
- Marcadores dos pontos A e B.

O traçado horizontal das ruas deverá seguir os dados reais importados sempre que possível.

Para as calçadas, priorize larguras verificadas. Na ausência desses dados, utilize dimensões configuráveis, classificadas como sintéticas.

Construa os elementos usando meshes simples, colliders adequados e materiais facilmente diferenciáveis.

Evite gerar objetos excessivamente fragmentados.

### Relevo

O terreno não deverá ser necessariamente plano.

Implemente a capacidade de receber dados reais de elevação, mas mantenha também a possibilidade de adicionar alterações artificiais.

A resolução de modelos de elevação públicos pode não ser suficiente para representar pequenos obstáculos. Portanto, rampas, degraus e buracos devem poder ser criados independentemente do modelo digital de elevação.

## 5. Elementos de acessibilidade

Crie componentes reutilizáveis para:

**Rampas**
- Comprimento.
- Largura.
- Elevação inicial e final.
- Inclinação longitudinal.
- Inclinação transversal.

**Buracos**
- Diâmetro ou dimensões.
- Profundidade.
- Posição.
- Severidade.
- Possibilidade de desvio.

**Obstáculos**
- Tipo: poste, árvore, lixeira, mobiliário ou objeto genérico.
- Área ocupada.
- Largura livre restante.
- Possibilidade de contorno.

**Degraus e meios-fios**
- Altura do desnível.
- Existência de rampa alternativa.
- Possibilidade de passagem conforme o perfil escolhido.

**Calçadas**
- Largura total e largura livre.
- Comprimento.
- Elevação.
- Inclinação.
- Regularidade e material do piso.
- Obstáculos associados.

Os parâmetros deverão ser editáveis no Inspector e, sempre que possível, por uma interface durante a execução.

## 6. Sistema de avaliação

Cada trecho deverá armazenar informações de acessibilidade.

Crie uma classe ou componente `AccessibilitySegment`, com propriedades como:

- Identificador.
- Comprimento.
- Largura livre.
- Elevação inicial/final.
- Inclinação longitudinal.
- Inclinação transversal.
- Irregularidade do piso.
- Obstáculos.
- Degraus.
- Estado de verificação.
- Custo de deslocamento.

Calcule a inclinação longitudinal:

`inclinacao_percentual = 100 * (elevacao_final - elevacao_inicial) / distancia_horizontal`

Considere separadamente subidas, descidas e inclinações transversais.

Use valores de referência configuráveis para diferentes perfis de mobilidade. Consulte a ABNT NBR 9050 e outras normas aplicáveis antes de tratar qualquer valor como critério normativo.

Inicialmente, utilize uma faixa livre mínima de referência de 1,20 m, permitindo ajustes e tratando casos especiais separadamente.

**Não confunda conformidade normativa com capacidade individual de uma pessoa atravessar um trecho.**

Classifique os segmentos em:

- Acessível.
- Acessível com dificuldade.
- Inacessível para o perfil selecionado.
- Não avaliado.

Não classifique dados ausentes automaticamente como acessíveis.

## 7. Simulação de trajetos

Implemente um grafo de circulação para pedestres, composto por nós e arestas.

Os nós poderão representar:
- Interseções de calçadas.
- Entradas e saídas de rampas.
- Travessias.
- Mudanças importantes de inclinação.
- Bifurcações.
- Origem e destino.

As arestas representarão trechos transitáveis, associados aos dados de acessibilidade.

Não utilize exclusivamente o NavMesh para determinar o melhor caminho. Ele poderá auxiliar na movimentação física, mas o cálculo principal deverá utilizar um grafo com custos personalizados.

Implemente Dijkstra ou A*.

O sistema deverá calcular pelo menos:

**Rota convencional:** prioriza a menor distância válida na rede.

**Rota acessível:** considera os impedimentos e os custos de dificuldade, podendo escolher um percurso maior.

Implemente uma função de custo configurável que utilize distância e penalidades por inclinação, piso, obstáculos e largura.

Barreiras consideradas intransponíveis pelo perfil deverão eliminar a aresta da busca, em vez de apenas acrescentar uma penalidade arbitrária.

Evite criar valores fixos sem justificativa. Organize os pesos em uma estrutura editável.

## 8. Perfis de mobilidade

Inicialmente, disponibilize:

1. Pedestre sem restrição simulada.
2. Pessoa utilizando cadeira de rodas manual.
3. Pessoa utilizando cadeira de rodas motorizada.

Cada perfil deverá possuir parâmetros independentes para largura necessária, capacidade de superar desníveis e tolerância a inclinações e irregularidades.

Os valores deverão ser apenas hipóteses de simulação até serem validados por normas e usuários reais.

## 9. Interação com o ambiente

Permita:

- Navegar pelo cenário com câmera livre.
- Alternar entre vista superior e perspectiva 3D.
- Selecionar origem e destino.
- Visualizar as rotas calculadas.
- Selecionar objetos e examinar suas propriedades.
- Alterar parâmetros de um obstáculo.
- Adicionar/remover obstáculos simulados.
- Recalcular a melhor rota após uma alteração.
- Alternar entre os perfis de mobilidade.

Crie um modo de inspeção em que a câmera acompanhe um marcador representando uma cadeira de rodas ao longo do trajeto.

Na primeira versão, o marcador poderá usar movimentação cinemática, sem simulação mecânica completa das rodas.

## 10. Visualização e interface

Exiba as rotas com cores:

- Verde: acessível no cenário.
- Amarelo: dificuldade relevante.
- Vermelho: bloqueado ou inacessível.
- Cinza: não avaliado.

Mostre a rota selecionada de forma destacada.

Adicione um painel que apresente:

- Origem e destino.
- Perfil selecionado.
- Distância da rota.
- Desnível acumulado de subida.
- Inclinação máxima encontrada.
- Número de obstáculos relevantes.
- Trechos problemáticos.
- Justificativa para a escolha do caminho.

Se uma rota não existir, informe o motivo, distinguindo ausência de conexão, obstáculos e dados insuficientes.

## 11. Cenários iniciais obrigatórios

Crie pelo menos cinco condições artificiais de teste sobre o mapa:

**Cenário 1 — Sem obstáculos:** ambas as rotas estão disponíveis.

**Cenário 2 — Degrau:** a rota mais curta contém um degrau intransponível para o perfil de cadeira de rodas.

**Cenário 3 — Inclinação:** uma rota contém uma rampa de 12%, enquanto uma alternativa mais longa tem inclinação menor.

**Cenário 4 — Buraco:** um buraco obstrui uma região da calçada e reduz a largura disponível.

**Cenário 5 — Obras:** uma calçada é bloqueada, obrigando o algoritmo a procurar outra passagem.

Todos os cenários são fictícios e não representam uma avaliação real da Avenida Vitória.

Permita alterná-los sem precisar modificar o código.

## 12. Arquitetura e desempenho

Separe responsabilidades em módulos como:

- `GeoDataImporter`
- `MapGenerator`
- `AccessibilitySegment`
- `AccessibilityObstacle`
- `AccessibilityProfile`
- `PedestrianGraph`
- `RouteCalculator`
- `ScenarioManager`
- `RouteVisualizer`
- `SimulationUI`
- `CameraController`

Esses nomes são sugestões e podem ser ajustados.

Evite recalcular todos os caminhos a cada frame. Atualize apenas quando a origem, destino, perfil, geometria ou condições relevantes forem alterados.

Utilize object pooling, meshes combinadas ou instanciamento controlado quando necessário.

Não comprometa a legibilidade do código por otimizações prematuras.

## 13. Desenvolvimento por etapas

**Etapa 1 — MVP**

Crie uma cena funcional, mesmo com geometria urbana provisória, contendo:

- Duas ou mais rotas entre A e B.
- Um obstáculo editável.
- Um trecho com inclinação.
- Um perfil de cadeira de rodas.
- Cálculo e visualização das rotas.
- Comparação entre menor caminho e caminho acessível.

**Etapa 2 — Integração geográfica**

Importe a geometria real da região da Avenida Vitória a partir de GeoJSON ou outra fonte adequada.

Documente a origem e a data dos dados.

**Etapa 3 — Obstáculos e relevo**

Adicione os componentes físicos e permita alterar os cenários durante a execução.

**Etapa 4 — Interface e validação**

Desenvolva o painel comparativo, diferentes perfis e testes automatizados de roteamento.

## 14. Critérios de sucesso

O protótipo será considerado funcional quando:

1. O usuário conseguir escolher dois pontos.
2. O programa conseguir encontrar uma rota.
3. Uma barreira intransponível impedir o uso de um trecho.
4. A existência de um caminho alternativo provocar novo roteamento.
5. O programa apresentar distância e fatores de dificuldade.
6. Alterações no cenário provocarem resultados coerentes.
7. O usuário conseguir verificar visualmente os obstáculos em 3D.

Crie testes de casos conhecidos para comprovar o funcionamento do algoritmo.

## 15. Entrega esperada

Quero um projeto Unity funcional e organizado, não apenas uma descrição conceitual.

Forneça os scripts C# necessários, prefabs ou geradores de geometria, configuração da cena e instruções para executar o protótipo.

Não use pseudocódigo no lugar de funcionalidades principais.

Caso o acesso a dados externos não esteja disponível, construa a primeira versão com uma rede sintética claramente identificada e prepare o importador para receber dados reais posteriormente.

Não alegue que o cenário reproduz fielmente a Avenida Vitória sem importar e conferir seus dados cartográficos.

Inclua os créditos dos dados OpenStreetMap e respeite suas condições de atribuição e licença.

**Comece pela Etapa 1, criando um teste realmente executável na Unity, mas organize a arquitetura para receber os dados reais nas etapas seguintes.**
