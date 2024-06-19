# Framework_respiratorio
Framework_respiratorio

Este é o arquivo de manual configuração para o aplicativo.

- qtd_amostras: Define o número de amostras a serem processadas em cada frame de áudio. Valores válidos incluem 512, 1024, 2048, 4096 e 8192. Ajustar este valor pode afetar a resolução do espectro de frequência e o desempenho do processamento.
  Exemplo: qtd_amostras=1024

- taxa_amostragem: Especifica a taxa de amostragem do áudio em Hz. Valores comuns são 22000, 44100, 48000 e 96000. Este parâmetro deve corresponder à taxa de amostragem do áudio que está sendo analisado.
  Exemplo: taxa_amostragem=48000

- hopsize : especifica o valor do hopsize em funcao do tamanho do frame. Os valores pode   variar de 0.01  a 0.99  que a porcentagem do tamanho do frame.

- delta: Um valor flutuante que define melhora a sensibilidade do thrashold (usado para controle da apneia) . Recomenda-se que varie entre 0 e 1.
  Exemplo: delta=0.25

- fftWindow: Define a janela a ser aplicada na Transformada de Fourier. Opções incluem:    Rectangular, Triangular, Hamming, Blackman, Hanning, Gaussian,Kaiser, Kbd, 
  BartlettHann, Lanczos, PowerOfSine, Flattop, Liftering.
  Exemplo: fftWindow=Hanning

- tipo_filtro: Escolhe o tipo de filtro a ser usado no pré-processamento do áudio. Opções incluem: ButterworthBandPassFilter, ChebyshevBandPassFilter, SimpleBandPassFilter, Hilbert, SemFiltro.
  Exemplo: filtro=MeuFiltroProgramado

- faixa_frequencia: Define a faixa de frequência do filtro selecionado. Deve ser especificado no formato Min-Max (ex: 1800-4000).
  Exemplo: faixa_frequencia=1800-4000

- quantidade_filterbank: Determina o número de coeficientes MFCC a serem gerados, com sugestões de valores sendo 13, 24 e 40.
  Exemplo: quantidade_filterbank=13

- frequencia_max_mel: Especifica a frequência máxima para o banco de filtros Mel. Recomenda-se usar a frequência máxima do filtro de pré-processamento ou a metade da taxa de amostragem.
  Exemplo: frequencia_max_mel=4000

- melFilterBankType: Permite escolher entre diferentes algoritmos de banco de filtros Mel. Opções são: ClassicMelFilterBank e AlternativeMelFilterBank.
  Exemplo: melFilterBankType=ClassicMelFilterBank

- mfccMetric: Define a métrica estatística a ser aplicada aos vetores MFCC. Opções incluem: mean (média), median (mediana), max (valor máximo) e min (valor mínimo).
  Exemplo: mfccMetric=mean


- portanto o arquivo configs.txt deve ser formado como o exemplo:

qtd_amostras=1024
taxa_amostragem=48000
hopsize = 0,5
delta=0,25
fftWindow=Triangular
filtro=Hilbert
faixa_frequencia=1800-4200
quantidade_filterbank=13
frequencia_max_mel=4200
filterBankType=Triangular
mfccMetric=median



Ajuste essas configurações no arquivo configs.txt conforme necessário para otimizar a análise de áudio. Certifique-se de salvar o arquivo após fazer as alterações e reiniciar o aplicativo para que as novas configurações tenham efeito.
