# Etapa 4 — interface e validação

O painel **Comparar trajetos → Trajetos** apresenta as duas buscas nas mesmas linhas: distância, subida/descida acumuladas, maiores inclinações direcionais e transversal, menor faixa livre contínua, quantidade de obstáculos e custo ponderado da rota acessível. O custo convencional é a própria distância; ele não é exibido como se fosse um custo de dificuldade comparável.

O desvio aparece em metros e porcentagem. A justificativa distingue barreiras no caminho convencional, dependência de dados desconhecidos, escolha por custo ou uso do mesmo caminho. Abaixo, cada rota informa separadamente os trechos difíceis, impedidos e não avaliados. Somente os quatro primeiros aparecem inicialmente; **Ver os … trechos problemáticos** expande a lista. Clique em um item para abrir seu editor e enquadrar a geometria.

**Três perfis** reúne os limites individuais de pedestre, cadeira manual e cadeira motorizada, além da distância ou motivo de falha para cada perfil na experiência corrente. **Usar este perfil** seleciona a hipótese. A comparação é guardada até mudar a rota; não executa buscas a cada repaint da interface. Os limites continuam editáveis nos assets do Inspector, sem declaração normativa ou clínica.

Na seleção de A/B, somente cliques que atingem um nó visível confirmam o ponto. **Cancelar seleção** ou **Esc** cancela. Esc também fecha o editor de trechos. Escolher o mesmo nó nos dois extremos resulta em distância zero, sem divisão por zero no percentual de desvio. Clicar em outro trecho abre suas propriedades a partir do início do painel.

Digitar em cotas, referência vertical ou caminho CSV bloqueia WASD/Q/E, T e Home enquanto o campo mantém foco. Clique no mapa para liberar o foco e voltar à navegação. Campos numéricos inválidos desabilitam a aplicação do trecho e mostram uma mensagem. Use ponto decimal. Os painéis e o viewport usam escala calculada a partir de largura e altura, preservando separação em formatos comuns de janela e em orientação vertical; em janelas muito pequenas, os textos também ficam menores.

## Velocidade de reprodução

A barra superior contém **Velocidade**, com botões **0,5×, 1×, 1,5×, 2×, 4× e 8×**. Use **Acompanhar rota acessível** no painel esquerdo e escolha um fator, antes ou durante o movimento. A troca é imediata e mantém a posição atual da cadeira. O padrão é 1×; parar, reiniciar ou recalcular a rota preserva a opção na cena atual. Trocar de mapa volta ao padrão.

O controle acelera somente o acompanhamento cinemático. As distâncias, dificuldades, perfis e resultados das rotas permanecem iguais; o tempo global e os controles de câmera não são acelerados. Os testes `PlaybackSpeedTests` cobrem mudança durante o movimento, seis multiplicadores, preservação ao parar/recalcular e chegada exata ao destino mesmo em alta velocidade.

## Critérios do pedido e evidências

| Critério | Evidência automatizada |
| --- | --- |
| Escolher dois pontos | Raycast nos colliders dos nós, confirmação/cancelamento e A=B em `InterfaceValidationTests` |
| Encontrar rota | Matriz de 2 cenas × 5 cenários × 3 perfis, com endpoints e soma de distâncias conferidos |
| Eliminar barreira intransponível | Nenhum arco da rota acessível pode estar impedido ou sem avaliação; degrau, rampa e buraco testados por perfil |
| Encontrar alternativa | Cadeiras contornam por 148 m no testbed; cerca de 352,69 m no modelo geográfico |
| Exibir distância e dificuldade | `ComparisonTests` confere métricas direcionais, desvio, justificativas e separação de desconhecido/impedido |
| Reagir às alterações | `WorldEditingTests` confere edição, desfazer/refazer, persistência, cotas e atualização de rota |
| Inspecionar obstáculos 3D | Malha e collider de buracos/degraus conferidos; foco de câmera por trecho; prévias geométricas da Etapa 3 |

`TestResults/stage4-editmode.xml` e `stage4-playmode.xml` são os resultados da suíte. As matrizes `Stage4-AvenidaVitoria_Testbed-Matrix.csv` e `Stage4-AvenidaVitoria_Geografico-Matrix.csv` guardam as 30 combinações no Editor. Nelas, perfil 0 é pedestre, 1 cadeira manual e 2 cadeira motorizada. O cenário é numerado de 1 a 5. Todas as distâncias pertencem ao modelo.

O executável possui uma verificação opcional **`-stage4-smoke`**, que percorre novamente a matriz nas duas cenas, verifica passabilidade e interdições, exercita seleção por raycast e registra erros de execução. Gera `Stage4-Player-Matrix.csv` e `Stage4-Player.txt` na pasta indicada por `-testbed-results`. No uso normal, esse componente não é criado. A execução de QA encerra o programa automaticamente.

Quando a cena ampliada até a FAESA está incluída, essa rotina também verifica seus 15 casos Praça–FAESA e 15 casos Ifes–FAESA: são 60 combinações no total, contando os 30 casos das cenas anteriores. A validação específica da expansão está em `EXPANSAO_FAESA.md` e nos relatórios `faesa-*`.

As tentativas de captura de tela usam o framebuffer real, incluindo IMGUI. Se a janela oculta produzir uma imagem preta, isso é registrado e a captura não é apresentada como validação visual. A conferência automática dos comandos de interface não substitui um ensaio manual de cliques, teclado ou usabilidade com pessoas que utilizam cadeira de rodas.

## Limites

O produto é um protótipo experimental com cartografia OSM e atributos de acessibilidade simulados. Nenhum teste comprova condições reais, conformidade com a NBR 9050 ou adequação individual de um trajeto urbano. A geometria de elevação, a avaliação de largura e a movimentação cinemática têm os limites descritos em `EDICAO_E_RELEVO.md` e `DADOS_GEOGRAFICOS.md`.

**© OpenStreetMap contributors — ODbL 1.0.** Fontes e licença em `CREDITOS.md` e `Assets/TrajetoInclusivo/Data/Geography`.
