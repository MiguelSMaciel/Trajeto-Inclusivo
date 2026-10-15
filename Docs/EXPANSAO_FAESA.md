# Expansão da Avenida Vitória até a FAESA

Nova cena: `Assets/TrajetoInclusivo/Scenes/AvenidaVitoria_FAESA.unity`. A cena original Praça–Ifes e o testbed sintético continuam disponíveis. O executável inicia no mapa ampliado.

A referência adotada é o **Campus Vitória da FAESA, Avenida Vitória, 2220, Monte Belo**, conforme [contato oficial](https://www.faesa.br/contato/), consultado em 15/10/2026. O recorte foi ampliado para leste, mantendo a origem local do Ifes. Não há novas coordenadas globais arbitradas manualmente.

## Fonte e construção da referência

O snapshot adicional está em `Assets/TrajetoInclusivo/Data/Geography/FAESA/avenida_vitoria_faesa.osm`, baixado em **15/10/2026, 16:00:51 UTC**, pela [API cartográfica OSM](https://api.openstreetmap.org/api/0.6/map?bbox=-40.323,-20.314,-40.308,-20.307). O SHA-256 e os parâmetros completos estão em `FAESA/provenance.json`. O download anterior continua preservado no diretório pai.

O OSM contém o edifício [way 51374617](https://www.openstreetmap.org/way/51374617), versão 3 de 08/10/2018, com `addr:street=Avenida Vitória`, `addr:housenumber=2220` e `addr:suburb=Monte Belo`. Ele não contém a sigla FAESA no nome. O endereço coincide com a referência institucional; isso não comprova a ocupação atual de cada edifício do complexo.

Não foi identificada uma entrada confiavelmente associada à FAESA nesse snapshot. Por isso, o destino **C** é uma **referência aproximada na calçada da Avenida Vitória**, obtida pela projeção do vértice do contorno desse edifício mais próximo das calçadas aproximadas da avenida. A projeção divide um trecho existente e reutiliza o nó resultante; não cria um acesso de comprimento zero nem uma passagem fictícia pelo interior de edifícios. O ponto não representa um portão, entrada acessível, centro do campus ou itinerário interno verificado.

Os limites do GeoJSON ampliado são longitude `[-40.3221,-40.313]`, latitude `[-20.3125,-20.3083]`. A exportação preserva **750 segmentos de eixos/caminhos, 1.212 edifícios, praça, campus Ifes e 30 referências pontuais**. A rede experimental gerada tem **1.608 nós e 2.522 trechos**, sem polígonos rejeitados. A triangulação bem-sucedida não comprova exatidão topográfica.

## Usar

Abra a cena nova e pressione Play, ou execute `Builds/Windows/TrajetoInclusivo.exe`. O padrão é **Praça → FAESA**. O botão **Ifes → FAESA** define o portão OSM do Ifes como origem. **Praça → FAESA** retorna à origem na praça; a seleção livre de A/B continua funcionando. Os botões para o recorte original e o testbed permitem alternar as três cenas.

Na barra central, **Aproximar: Praça / Ifes / FAESA (ref.)** enquadra um marco sem alterar A/B, obstáculos ou avaliação. Os mesmos marcos têm rótulos clicáveis sobre o mapa, com linhas de ligação, identificação de origem/destino e posicionamento para evitar sobreposição. Quando um marco à frente da câmera fica fora do enquadramento, o rótulo é limitado à borda da área útil e indica **fora da vista**; marcos atrás da câmera permanecem disponíveis pelos botões da barra. Esses rótulos não são geometrias geográficas novas nem dados sobre acesso.

Trocar entre os botões de trajeto Praça–FAESA e Ifes–FAESA enquadra automaticamente o percurso após o recálculo. Na falta de uma rota acessível confirmada, o enquadramento usa a convencional disponível. Aproximar um marco interrompe o acompanhamento cinemático e cancela a seleção pendente de A/B. Cliques nos rótulos e no rodapé de créditos são tratados como interface, evitando selecionar o pavimento por trás.

Em vistas geográficas próximas (distância de câmera inferior a 130 m), os grandes textos desenhados no chão ficam ocultos, preservando os rótulos da interface e as geometrias. Ao afastar a câmera, os textos originalmente habilitados reaparecem. O critério é apenas de apresentação; não muda nomes, posições ou custos da rede.

O mapa ampliado começa **não avaliado**. Ative explicitamente as hipóteses de teste para comparar rotas acessíveis simuladas. Os cinco presets da nova cena ficam em `Data/Geography/FAESA/Scenarios`; seu trecho artificial foi escolhido no caminho Praça–FAESA com alternativa conectada. Dependendo da origem selecionada, um preset pode não afetar o caminho Ifes–FAESA. Use a avaliação efetiva do trecho, em vez de supor que todo preset altere qualquer trajeto escolhido.

Perfis, editor físico, câmera, histórico e persistência são os mesmos das outras cenas. Experiências JSON de mapas anteriores não são compatíveis com o novo grafo e são recusadas pela validação; use cada arquivo na sua cena de origem. A grade de elevação artificial original mantém sua cobertura, que pode não alcançar todos os nós novos. Fora dela, as cotas anteriores permanecem; importe uma grade mais ampla se necessário.

Na base plana dos presets, o percurso convencional Praça–FAESA mede **654,388 m**, e Ifes–FAESA mede **478,373 m**, no grafo experimental. O desvio do cenário de degrau Praça–FAESA mede cerca de **665,434 m**. Esses números não descrevem distâncias aferidas em um trajeto público acessível. As matrizes completas estão em `TestResults/FAESA-Matrix.csv`; os 54 testes de EditMode e 9 de PlayMode foram aprovados, incluindo os testes anteriores.

O executável Windows também passou por 60 combinações nas três cenas, incluindo 30 até a FAESA, sem erros de execução. Relatórios e capturas da interface estão em `TestResults/FAESA` e foram copiados para `Builds/Windows/Documentacao/Resultados/FAESA`.

## Reproduzir e distribuir

Execute `Tools/Export-FaesaGeoJson.ps1` para reexportar o snapshot local. O parâmetro `DownloadedAtUtc` registra a data do snapshot desta entrega; informe a nova data caso substitua o arquivo por outro download. Depois use **Trajeto Inclusivo → Gerar cena ampliada até a FAESA** no Editor. Esse gerador substitui somente a cena, os cenários e as malhas de demonstração da expansão; preserve suas cópias editadas antes de regenerar.

**Gerar executável Windows** inclui as três cenas, abre a ampliada e distribui o snapshot, GeoJSON, proveniência, grafo e configurações em `Builds/Windows/MapData/FAESA`. Métodos de derivação e scripts de exportação acompanham `MapData`; documentação acompanha `Documentacao`.

As calçadas deslocadas dos eixos, conexões de junção, larguras, cotas e obstáculos continuam hipóteses de simulação. Não foi feito levantamento de acesso, relevo, obras ou circulação no campus. O programa funciona offline após a geração.

**© OpenStreetMap contributors — ODbL 1.0.** Dados e banco derivado são distribuídos com a licença e os créditos. O site da FAESA confirma o endereço institucional; não é a fonte das geometrias OSM.
