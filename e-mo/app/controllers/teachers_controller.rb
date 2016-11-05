class TeachersController < ApplicationController
  before_action :access_filter
  skip_before_action :access_filter, only: [:expression_data]
  skip_before_action :authenticate_user!, only: [:expression_data]
  protect_from_forgery except: [:expression_data]

  def index
    @chart_data = []
    50.times do |i|
      @chart_data.push([i, rand * 100])
    end
  end

  # FaceTrackingアプリでのPOST送信先
  def expression_data
    p "スマイルデータ： #{params['smileData']}"
    expression_json = {smile: params["smileData"]}.to_json
    SendExpressionDataJob.perform_later(expression_json)
    head 200
  end

  private
    def access_filter
      if current_user.teacher_flag
        # 先生でログイン
      else
        # 生徒でログイン
        redirect_to student_url
      end
    end
end
