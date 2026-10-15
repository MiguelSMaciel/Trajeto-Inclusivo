# Etapa 3 — edição, obstáculos e elevação

As duas cenas possuem um editor em execução. As alterações são hipóteses locais e não constituem avaliação da Avenida Vitória. A cena geográfica continua começando sem dados de acessibilidade.

## Editar e guardar uma experiência

Clique no pavimento para abrir o painel direito. Edite cotas inicial/final, largura, inclinação transversal, irregularidade e ondulação física. A inclinação alvo calcula a cota final usando a distância horizontal existente; o botão **Aplicar trecho / rampa simulada** confirma o conjunto. Os endpoints são compartilhados: mover uma cota altera todos os trechos incidentes. O comprimento vem da posição dos nós; pode ser modificado pelo Transform no Editor. `RampModule.prefab` é um módulo reutilizável de 4 m com seus endpoints.

Na seção de obstáculos, escolha poste, árvore, lixeira, mobiliário, objeto, degrau, buraco ou obras. Adicione, selecione na lista, ajuste os parâmetros e aplique. Posição longitudinal varia de 0 a 1; deslocamento lateral positivo fica à direita de quem percorre o trecho do início ao fim. A área ocupada é limitada à faixa do trecho. “Interdita passagem sem contorno” com “Permite contorno” desmarcado representa fechamento físico de ambas as buscas.

**Desfazer / Refazer** retém até 12 comandos, incluindo troca de cenário, aplicação de grade e carga de arquivo. Trocar de cenário restaura larguras e obstáculos do preset; uma grade aplicada continua sendo a base de elevação. Desfazer a troca recupera a experiência anterior. As escolhas de A/B e perfil são salvas, mas não criam comandos separados no histórico.

**Salvar experiência** grava um JSON na pasta `Application.persistentDataPath`, com o nome `<cena>.experiment.json`. No Windows, normalmente fica em `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/`; a mensagem mostra o caminho efetivo. **Carregar experiência** lê esse arquivo. O JSON guarda parâmetros, obstáculos, cotas, base de elevação, grade, proveniência vertical, perfil, cenário, A/B e estado de avaliação. Não modifica os assets originais.

A carga valida versão do formato, identidade do mapa/grafo, IDs, valores finitos e limites antes de alterar a cena. Mapas distintos são recusados. Gravação usa arquivo temporário e substituição; erros aparecem na barra. O limite de carga é 8 MiB, com até 32 obstáculos por trecho e 5.000 no total.

## Geometria e largura

O buraco escava o próprio pavimento: fundo, paredes e superfície usam uma malha com `MeshCollider` correspondente. Buracos sobrepostos usam a maior profundidade. A interface limita profundidade a 0,25 m. A escavação afeta somente o trecho selecionado; não recorta ruas, edifícios ou outras malhas sobrepostas. Guias de conexão hipotética sem pavimento não aceitam buraco.

Degraus têm base e topo acompanhando o plano inclinado da calçada e altura vertical configurável. Com ondulações, essa peça usa a interpolação de seus quatro cantos. Postes, árvores e bancos usam volumes simples com colliders. A área retangular ocupada é conservadora, inclusive para troncos cilíndricos.

A largura livre é calculada ao longo do trecho. Nos intervalos onde os obstáculos coexistem, suas projeções laterais são unidas; considera-se o maior intervalo contínuo restante. Duas passagens separadas não são somadas. O gargalo é limitado também pela largura livre base. Degraus abaixo da capacidade do perfil não reduzem a passagem; degraus maiores exigem contorno suficiente ou bloqueiam a aresta.

Esse cálculo representa gargalos locais; não prova que exista uma trajetória lateral contínua entre gargalos sucessivos. Não calcula giro, balanço, manobras ou volume varrido de uma cadeira. O marcador continua cinemático sobre o eixo da rede, sem desviar fisicamente dos obstáculos.

Ondulações usam amplitude, período e envoltória que zera nos endpoints; a malha é subdividida e seu collider acompanha a superfície. A avaliação traduz amplitude em irregularidade por uma hipótese explícita: `max(irregularidade, clamp01(amplitude / 0,05))`. A inclinação de roteamento e a distância continuam sendo as dos endpoints, sem integrar microdeclives ou o comprimento ondulado.

## Importar elevação

Há uma grade **artificial de demonstração**, identificada como simulada, em `Resources/ExampleElevation.asset`, e o CSV correspondente em `Data/Elevation/example_local_simulated.csv`. Ela não é um DEM de Vitória.

Para importar valores externos, prepare uma grade regular completa, recortada e reamostrada no QGIS ou ferramenta equivalente. Formato UTF-8, ponto decimal, separador vírgula:

```csv
x,z,elevation
0,0,100.18
10,0,100.38
0,10,100.28
10,10,100.48
```

X é leste e Z é norte, ambos em metros relativos à mesma origem horizontal da cena. **O importador não reprojeta nem identifica automaticamente o sistema de referência.** Use a origem geográfica registrada em `Docs/DADOS_GEOGRAFICOS.md`; um arquivo com origem diferente pode sobrepor posições erradas mesmo que seus números sejam válidos. A cena sintética usa sua própria origem de teste.

`elevation` representa a cota da superfície de circulação. Informe o datum/referência vertical e uma origem vertical para subtrair antes da conversão para float. No exemplo acima, uma origem vertical de 100 produz Y entre 0,18 e 0,48. Ao usar um DEM de terreno, prepare explicitamente a relação entre terreno, calçada e datum; a aplicação não a deduz.

No painel, informe caminho local do CSV, referência vertical, origem vertical e se os dados são simulados. **Carregar e aplicar CSV local** faz interpolação bilinear nos nós cobertos. Fora da cobertura, preserva as cotas anteriores. São recusados NaN/infinito, duplicatas, lacunas, espaçamento irregular, grade menor que 2×2 ou maior que 65.536 amostras. Dados reais importados recebem estado **ImportedUnverified**, sem habilitar automaticamente avaliação de acessibilidade nem atestar levantamento de campo.

No Editor, **Trajeto Inclusivo → Importar grade CSV de cotas locais** também permite salvar um `ElevationDataset` com fonte, data e referência vertical. Guarde licença e metadados da fonte junto ao arquivo original.

Aplicar a grade também deforma as malhas de contexto nos seus vértices e cria uma superfície de referência 0,40 m abaixo da cota de circulação. Isso evita que o plano artificial tampe pequenos defeitos; não representa uma separação terreno/calçada medida. Malhas extensas e intervalos entre nós seguem aproximações lineares, e bordas da cobertura podem ter transições abruptas. A fonte original é mantida para restauração por desfazer/carga.

Grades permanecem na base ao trocar cenário; o deslocamento artificial do cenário de inclinação é somado à nova base. Assim, **12% é o resultado do preset sobre a base original**; após aplicar outras cotas, consulte a inclinação efetivamente calculada. O programa funciona offline e não baixa modelos de elevação.
