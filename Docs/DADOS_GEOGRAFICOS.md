# Dados geográficos — Etapa 2

A cena ampliada `AvenidaVitoria_FAESA` usa um segundo snapshot e conserva a mesma origem local. Seus parâmetros, a correspondência de endereço e a referência aproximada na calçada estão em `EXPANSAO_FAESA.md`. O snapshot e a cena originais descritos abaixo continuam preservados.

## Snapshot e referências

Recorte OpenStreetMap baixado em **15/10/2026, 14:33 UTC**. Original: `Assets/TrajetoInclusivo/Data/Geography/jucutuquara.osm`. URL, SHA-256, limites, origem e contagens estão em `provenance.json`. Nenhum serviço é consultado durante a execução.

- Praça: [way 541652953](https://www.openstreetmap.org/way/541652953), `Praça Asdrúbal Soares`, versão 3, editada em 25/04/2019.
- Ifes: [way 1352259307](https://www.openstreetmap.org/way/1352259307), versão 4, editada em 22/06/2026, endereço Avenida Vitória, 1729; nome original `IFES - Campus VItória`.
- Destino: [portão node 12511661225](https://www.openstreetmap.org/node/12511661225), pertencente ao contorno do Ifes. Isso não comprova o acesso público atual ou sua acessibilidade.
- Origem local: nó `12511666338`, longitude **-40.3194586**, latitude **-20.3113408**, extraído do contorno do Ifes. Não há altitude de referência medida.
- A: centroide geométrico da praça; B: coordenada do portão. Ambos se ligam à calçada aproximada por acessos sintéticos, projetados no trecho próximo.

A Prefeitura identifica a [Praça Asdrúbal Soares como Praça de Jucutuquara](https://www.vitoria.es.gov.br/noticia/praca-de-jucutuquara-recebera-melhorias-20544). O [Ifes confirma o endereço do campus](https://ifes.edu.br/contato/24-campus-vitoria). Essas fontes verificam a identidade dos locais, sem fornecer levantamento das calçadas. A Prefeitura também publicou [intervenções na praça em 2025](https://www.vitoria.es.gov.br/noticia/praca-de-jucutuquara-esta-com-60-por-cento-das-intervencoes-concluidas-52418): o polígono OSM de 2019 pode estar desatualizado.

## Conversão reproduzível

`Tools/Export-OsmGeoJson.ps1` usa somente PowerShell e XML/JSON locais. Recorta eixos no retângulo longitude [-40.3221,-40.3166], latitude [-20.3125,-20.3083], preservando nós compartilhados, nomes, tags, IDs, versões e datas. Endpoints interpolados nos limites recebem IDs exclusivos e não são ligados por proximidade.

Foram exportados **496 segmentos de eixos/caminhos/escadarias, 787 footprints de edificações, 1 polígono de praça, 1 de campus e 18 pontos de referência**. Edificações permanecem inteiras quando algum vértice cai no recorte. Relações multipolígono não são reconstruídas; isso pode deixar lacunas. O snapshot não constitui um cadastro completo.

O gerador projeta os eixos em metros locais, triangula os footprints (inclusive polígonos côncavos simples) e combina as malhas por camada. Polígonos inválidos são rejeitados e contabilizados. O relatório está em `TestResults/GeographicSetup.txt`.

## Geometria importada e hipóteses

| Elemento | Origem / situação |
| --- | --- |
| Eixos horizontais e nomes | OSM, não verificados em campo |
| Contornos de edifícios, praça e campus | OSM, versões preservadas |
| Altura de edifícios | Tag OSM não verificada quando presente; senão níveis × 3 m ou padrão de 6 m; representação limitada a 2–50 m |
| Largura viária | Tag width numérica não verificada; senão lanes × 3,20 m ou hipótese de 6 m |
| Calçadas | Deslocamento lateral aproximado; largura padrão sintética de 2,40 m |
| Travessias e conexões | Ligações de teste em junções/nós compatíveis; não comprovam travessias reais |
| Cotas, inclinações, piso e obstáculos | Sem levantamento; hipóteses artificiais somente no modo de teste |

As tags `sidewalk=left/right/no/separate` e `sidewalk:left/right=no` são consideradas. `oneway` de veículos não é imposto a pedestres. Escadarias, paths, acessos privados, pontes e túneis não são incluídos na rede experimental nesta versão. Isso torna a rede parcial; uma falha de conexão no modelo não comprova impossibilidade no mundo real.

Conexões exigem identificador de nó OSM e nível compatíveis. Linhas geometricamente cruzadas não criam arestas. Nos nós de forma, as calçadas do mesmo lado continuam; ligações hipotéticas de travessia são limitadas a junções ou nós de cruzamento mapeados. Ainda assim, não há comprovação de rampa, semáforo, refúgio ou condições atuais. As conexões aparecem como linhas com hitboxes de seleção, sem inventar meio-fio físico para elas.

A avaliação começa desconhecida: `hasAssessmentData=false`. “Ativar hipóteses de teste” preenche um cenário artificial completo e mantém `verification=Simulated`. Desativar retorna à situação não avaliada. A tag `wheelchair=yes` do campus não é propagada como avaliação de seus acessos ou das calçadas.

## Coordenadas e precisão

`GeoDataImporter.Import` aceita FeatureCollection com Point, MultiPoint, LineString, MultiLineString, Polygon e MultiPolygon. Preserva propriedades escalares e ordem das partes. GeometryCollection não é suportado. O gerador OSM usa ways simples; não triangula furos de relações.

Para WGS84, longitude vem antes de latitude. A prévia usa UTM 24S, meridiano central -39° e elipsoide GRS80, aproximando WGS84/SIRGAS para o contexto local. Não substitui transformação temporal de datum, ajuste topográfico ou operação vertical rigorosa. Para levantamento preciso, reprojete no QGIS para EPSG:31984 com operação adequada e pontos de controle.

Coordenadas globais ficam em double até subtrair a origem local; depois viram Vector3. X é leste, Z norte e Y altitude. Sem terceiro número, Y=0 é apenas plano de desenho e `hasElevation=false` preserva a ausência. Não se afirma declividade real a partir dessa substituição.

Os testes conferem a coerência com o snapshot, identidades, checksum, triangulação e conectividade. Não houve inspeção de campo ou conferência contra levantamento independente. O testbed original permanece inteiramente sintético e separado.

## Elevação na Etapa 3

A entrega inclui importação de grade CSV local e um exemplo artificial, sem acrescentar um DEM real ao snapshot OSM. Cotas da grade são superfícies de circulação, em metros após subtração da origem vertical informada. É responsabilidade da preparação GIS usar a mesma origem X/Z e registrar a referência vertical. Não há detecção automática de CRS nem transformação de datum vertical. A importação não preenche largura, piso, obstáculos ou demais dados ausentes de acessibilidade.

`Docs/EDICAO_E_RELEVO.md` descreve formato, interpolação, cobertura, proveniência, persistência e limitações da geometria de referência. As alterações de relevo e obstáculos feitas na interface são simuladas; a grade externa pode ser classificada como importada não verificada. Nenhuma dessas ações declara condições reais de circulação.
